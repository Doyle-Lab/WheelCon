

clear 

folder = '/Users/huahua/Documents/MATLAB/trade_offs/';

file_names = 'subject1_SAT_plan.txt';

cd(folder);
M = dlmread(file_names,',',1,0); 


% load the data
t = M(:,1);
trail = M(:,2);

bump = M(:,3);
quant_act = M(:,4);  
delay_act = M(:,5);   
delay_vis = M(:,6);  
quant_vis = M(:,7); 

error = M(:,8);  
control = M(:,9);  


T = 30;     % the time for each block

block = floor(max(t)/T);
inf_error_block = zeros(block,1);
inf_error_block_2s = zeros(block,1);

for ii=1:block
    sel = find(t>(ii-1)*T+10 & t<=ii*T-0.1);  % exclude the first 10 second for the learning effects or random effects
    inf_error_block(ii) = max(abs(error(sel)));
    
    for jj=1:10
        sel = find(t>(ii-1)*T+9+2*(jj-1) & t<=(ii-1)*T+9+2*jj);
        inf_error_block_2s(ii,jj) = max(abs(error(sel)));     % infinity norm of error
        inf_control_block_2s(ii,jj) = max(abs(control(sel))); % infinity norm of control
    end
    
    
end

Font_name = 'Times New Roman';

internal_error = min(inf_error_block);

Figure = figure('color',[1 1 1]);
set(Figure, 'Position', [100 100 600 500]);
subplot(111); hold on;
plot([1:7], inf_error_block(1:7)-internal_error, 'ro-', 'LineWidth',2);  % Quant
plot([1:7], inf_error_block(8:14)-internal_error, 'bo-', 'LineWidth',2) % delay
plot([1:7], inf_error_block(15:21)-internal_error, 'ko-', 'LineWidth',2) % both
plot([1:7], inf_error_block(1:7)+inf_error_block(8:14)-2*internal_error, 'ko--', 'LineWidth',2) % sum
legend('Quant','Delay','Both','Sum');
title('SATs in plan layer'); xlim([1,7]);
xlabel('R (bit)'),  ylabel('infinity norm x(t)');
set(gca, 'fontsize', 30, 'FontName', Font_name);

