%% Design path for the mountain-bike game
%%
% It generates the TEXT file: Time, trail, ActionQuant, ActionDelay?Bump, VisionDelay
% It will show the designed trail, bump, and designed delay
% 
%
% Quanying Liu
% Sep 25, 2018 
%

%% For testing the vision delay and action delay
% filename_output = 'setting_Vdelay_Adelay_worstcase.txt';
% isworstcase = 1
% bumptorque = 0;
% ActionQuant = [10*ones(1,12)];
% ActionDelay = [0*ones(1,6) 0:0.15:0.75];
% VisionDelay = [0:0.15:0.75 0*ones(1,6)];
% VisionQuant = [10*ones(1,12)];


%% For testing the SATs in trail
% filename_output = 'setting_SAT_worstcase_trail.txt';
% isworstcase = 1;
% bumptorque = 0;
% %ActionQuant = [1:7 10*ones(1,7) 1:7];  % 1-10
% ActionQuant = [10*ones(1,21)];  % 1-10
% ActionDelay = [0*ones(1,21)];  % 0
% VisionDelay = [-0.8*ones(1,7) -0.8:0.2:0.4 -0.8:0.2:0.4];  % in seconds
% VisionQuant = [1:7 10*ones(1,7) 1:7];

%% For testing the SATs in bump
% filename_output = 'setting_SAT_worstcase_trail_new.txt';
% isworstcase = 1;
% bumptorque = 100;
% % ActionQuant = [1:7 10*ones(1,7) 1:7];  % 1-10
% ActionQuant = [10*ones(1,21)];  % 1-10
% ActionDelay = [0*ones(1,21)];  % 0
% VisionDelay = [-0.8*ones(1,7) -0.8:0.2:0.4 -0.8:0.2:0.4];  % in seconds
% VisionQuant = [1:7 10*ones(1,7) 1:7];


%% For testing the bump and trail
% filename_output = 'setting_Bump_QT.txt';
% isworstcase = 1;
% bumptorque = 100;
% ActionQuant = [10];
% ActionDelay = [0];
% VisionDelay = [-1];
% trail_angle = 45;
% VisionQuant = [10];
%
% M_bump = design_path_MountainBike_main(filename_output, isworstcase, bumptorque, ActionQuant, ActionDelay, VisionDelay,80);
% M_trail = design_path_MountainBike_main(filename_output, isworstcase, 0, ActionQuant, ActionDelay, VisionDelay);
% M_all = [M_bump; M_trail; M_trail];
% M_all(6001:9000,3) = M_bump(:,3);
% M_all(6001:9000,2) = M_bump(:,2)+M_trail(:,2);
% M = [M_all; M_all; M_all; M_all];
% M(:,1) = 0.01:0.01:360;

function M = design_path_MountainBike_main(filename_output, isworstcase, bumptorque, ActionQuant, ActionDelay, VisionDelay, VisionQuant, trail_angle)
    % filename_output = 't_path_w_sharpturn_MountainBike.txt';
    if nargin < 8
        trail_rand = 1;
    else
        trail_rand = 0;
    end
    if nargin < 7
        VisionQuant = 10*ones(size(VisionDelay));
    end
    
    if length(ActionQuant)~=length(ActionDelay)
        error('The Quantization and Delay for Action should be the same length.');
    end
    
    if length(ActionDelay)~=length(VisionDelay)
        error('The Action Delay and Vision Delay should be the same length.');
    end
    
    if bumptorque>100
        bumptorque = 100;
    end
    
    block = length(ActionQuant); % number of blocks


    % ---------------------------------------------------------
    % define parameters
    % 
    % In our game, the forward_speed is 2.5 units / s  
    % x is -10 to 10
    %
    
    T = 30;     % the time for each block

    time_resolution = 0.01;  % resolution for time - 0.01 second
    
    dy = 2.5*time_resolution;
    
    N = T/time_resolution;
    
    t = time_resolution:time_resolution:block*T;  % each 30 seconds for one parameter setting, sampling rate = 10hz (0.01s)

    

    %% worstcase
    if isworstcase==1
        % Each 1.5 second, there is a turn
        % Each 2 second, there is a bump
        % bump and turn is independent
        % ----------------------------------
        % set up the bump for the worst case
        if bumptorque==0
            bump = zeros(1, block*N);
        else
            bump_block = zeros(1, N);
            
            t_bump_last = 0.5/time_resolution;  % bump lasts for 0.5 seconds
               
            direction_bump = 1;
            for trail_i=1:15
                t_bump_onset = randperm(200-t_bump_last, 1);   % random the onset time of a bump
                bump_block(200*(trail_i-1)+t_bump_onset+1 : 200*(trail_i-1)+t_bump_onset+t_bump_last) = bumptorque*direction_bump;
                direction_bump = -direction_bump;
            end
            
            bump = repmat(bump_block, 1, block);
        end
        
        % ----------------------------------
        % set up the trail for the worst case
        if trail_rand==1  % random angle
            trail_block = zeros(1, N);
            direction_trail = 1;
            x_ini = 0;
            for trail_i=1:20
                angle = pi*(10+35*rand(1,1))/180;  % uniform distribution: 10~45 degree
                dx = direction_trail * dy / tan(angle);

                trail_block(150*(trail_i-1)+1 : 150*trail_i) = x_ini+[1:150].*dx;  % 1.5s
                x_ini = trail_block(150*trail_i);

                direction_trail = -direction_trail;
            end
            trail_block = trail_block-mean(trail_block);  % demean
            
            if max(trail_block)>10 | min(trail_block)<-10 % normalize to -10 to 10
                trail_block = 20/(max(trail_block)-min(trail_block))*(trail_block-min(trail_block))-10; 
            end
            
            trail = repmat(trail_block, 1, block);

            % ----------------------------------
            % set up the Delay and Quant for the worst case
            quant_act = [];
            delay_act = [];
            delay_vis = [];
            quant_vis = [];
            for block_i=1:block
                quant_act = [quant_act ActionQuant(block_i)*ones(1,N)];
                delay_act = [delay_act ActionDelay(block_i)*ones(1,N)];
                delay_vis = [delay_vis VisionDelay(block_i)*ones(1,N)];
                quant_vis = [quant_vis VisionQuant(block_i)*ones(1,N)];
            end
        
        else   % selected angle
            trail_block = zeros(1, N);
            direction_trail = 1;
            x_ini = 0;
            for trail_i=1:20
                angle = pi*trail_angle/180;  % fixed trail_angle
                dx = direction_trail * dy / tan(angle);

                trail_block(150*(trail_i-1)+1 : 150*trail_i) = x_ini+[1:150].*dx;  % 1.5s
                x_ini = trail_block(150*trail_i);

                direction_trail = -direction_trail;
            end
            trail_block = trail_block-mean(trail_block);  % demean
            
            if max(trail_block)>10 | min(trail_block)<-10 % normalize to -10 to 10
                trail_block = 20/(max(trail_block)-min(trail_block))*(trail_block-min(trail_block))-10;
            end
            
            trail = repmat(trail_block, 1, block);

            % ----------------------------------
            % set up the Delay and Quant for the worst case
            quant_act = [];
            delay_act = [];
            delay_vis = [];
            quant_vis = [];
            for block_i=1:block
                quant_act = [quant_act ActionQuant(block_i)*ones(1,N)];
                delay_act = [delay_act ActionDelay(block_i)*ones(1,N)];
                delay_vis = [delay_vis VisionDelay(block_i)*ones(1,N)];
                quant_vis = [quant_vis VisionQuant(block_i)*ones(1,N)];
            end
        end
        
    end

%     %% stochastic-case
%     if worstcase~=1
% 
%         % ------------------------------------------------------
%         % set up the bump for the stochastic case
%         if bumptorque==0
%             bump = zeros(1, block*T/time_resolution);
%         else
%             bump_block = zeros(1, T/time_resolution);
%             bump = [];
% 
%             for j=1:block
% 
%                 mu = 1;
%                 L = exprnd(mu, N, 1)*100;   % exponential distribution for the timing
% 
%                 v = sum(L)/T;   % T seconds in total
%                 Y = [0.01:0.01:T]*v;
% 
%                 direction_all = [1*ones(1,N) -1*ones(1,N)];
%                 direction_all = Shuffle(direction_all);  % random the direction
%                 for i=1:N
%                     direction = direction_all(i);
% 
%                     if i==1
%                         select_t = find( Y<=L(1) );
%                         bump_block(select_t(1):select_t(1)+50) = direction * force;  % last 0.5 second
%                     else
%                         select_t = find( Y>sum(L(1:i-1)) & Y<=sum(L(1:i)) );
%                         bump_block(select_t(1):select_t(1)+50) = direction * force;  % last 0.5 second
%                     end
%                 end
%                 bump_block = bump_block(1:length(t)/10);
%                 bump = [bump bump_block];
%             end
%         end
%         
%         
%         
%     end
    % 6 columns: time, trail; quant_act, delay_vis, bump, delay_act
    M = [t' trail' bump' quant_act' delay_act' delay_vis' quant_vis'];
    
    dlmwrite(filename_output, M);  
    
    Figure = figure('color',[1 1 1]);
    set(Figure, 'Position', [100 100 600 1200])
    FN = 'Times New Roman';
    subplot(611); hold on; plot(M(:,1), M(:,2), 'k-', 'LineWidth', 2); ylabel('trail');
    subplot(612); hold on; plot(M(:,1), M(:,3), 'k-', 'LineWidth', 2); ylabel('bump'); ylim([min(M(:,3))-10, max(M(:,3))+10]);
    subplot(613); hold on; plot(M(:,1), M(:,4), 'k-', 'LineWidth', 2); ylabel('quant act');
    subplot(614); hold on; plot(M(:,1), M(:,5), 'k-', 'LineWidth', 2); ylabel('delay act');
    subplot(615); hold on; plot(M(:,1), M(:,6), 'k-', 'LineWidth', 2); ylabel('delay vis');
    subplot(616); hold on; plot(M(:,1), M(:,7), 'k-', 'LineWidth', 2); ylabel('quant vis');
    
end