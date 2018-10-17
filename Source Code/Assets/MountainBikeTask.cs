using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/**************************************************
 * Programmed by Quanling Liu and Ahkeel Mohideen *
 *     at California Institute of Technology      *
 *                    in 2018                     *
 *************************************************/

public class MountainBikeTask : MonoBehaviour
{

    /************************************************************************************************************************
     *                                       Mountain Bike Task                                                             *
     *                                                                                                                      *
     * Input File Format:                                                                                                   *
     * Time, Path Angle, Bump Size, Action Quantization, Action Delay, Vision Delay, Vision Quantization                    *
     * Output Saved as:                                                                                                     *
     * Time, Path Angle, Bump Size, Quantization, Action Delay, Vision Delay, Vision Quantization, Trail Error, Wheel Angle *
     ***********************************************************************************************************************/


    /* Variables for File Interaction */
    public static string inputFilePath; /* path to input file */
    private string saveFilePath; /* path to output file */
    private List<string> errorData = new List<string>(); /* holds error information of each cycle in a formatted string */
    public static string RecordFileName;   /* name of output file */
    private string gameDate; /* current game date to append to output file name */

    /* Horizontal Axis */
    public LineRenderer horizontalLine;
    private float horizontalLineWidth = 0.1f; 

    /* player line of constant width and height but variable x-position */
    public LineRenderer playerLine;
    private float playerLineHeight = 1f;
    private float playerLineWidth = 0.05f;
    private float playerLineXPos = 0; 

    /* path line of variable x-position, y-position, and length */
    public LineRenderer pathLine;
    private float pathLineHeight = 0.5f;
    private float pathLineWidth = 0.1f;
    private float pathLineXPos;
    private float pathPositionXPos;
    
    /* Variables for Screen Interaction */
    private static float sensitivity; /* sensitivty constant of wheel */
    private const float XminScreen = -10f; 
    private const float XmaxScreen = 10f;
    private const float YminScreen = -3.5f;
    private const float YmaxScreen = 6.5f;
    private const float totalAngleRange = 180f; /* accepted angle span of wheel */ 
    private float dy_per_dt = 2.5f; /* forward speed */

    /* Variables for Keeping Time */
    private int gameLength; /* length of the current game in play */
    private float gameTime; /* current game time in seconds */
    private Boolean gameIsRunning; /* true: game is currently playing, false: game is paused or over */

    /* Variables for Accessing and Controlling the Wheel */
    private int deviceIdx = 0; /* corresponds to first wheel plugged into the computer */
    private string wheelProp; /* use to display properties of system */
    private bool logiIni; /* use to check if wheel is working */
    private float normalizedAngle; /* angle of the wheel in degrees */
    private LogitechGSDK.DIJOYSTATE2ENGINES rec; /* current state of the wheel */
 
    /* Variables for Quantization */
    private int actionQuantizationLevel; /* current action quantization level 1 --> 10 */
    private int visionQuantizationLevel; /* current vision quantization level 1 --> 10 */
    private float quantizedAngle; /* quantized wheel angle value */
    
    /* Animation Curves for */
    private AnimationCurve trailPositionCurve; /* x-displacement of trail with respect to center */
    private AnimationCurve actionQuantizationCurve; /* positive integer quantization level */
    private AnimationCurve visionDelayCurve; /* vision delay in seconds */
    private AnimationCurve bumpSizeCurve; /* bump (force feedback) size */
    private AnimationCurve actionDelayCurve; /* action delay in seconds */
    private AnimationCurve visionQuantizationCurve;


    /* Text Notifications for Game Screen */
    public Text timeNotification;
    public Text actionQuantizationNotification;
    public Text visionDelayNotification;
    public Text actionDelayNotification;
    public Text visionQuantizationNotification; 

    /* Variables for Bumps */
    private float bumpSize;

    /* Variables for Action Delay */
    private List<float[]> wheelAngles = new List<float[]>(); /* list of float arrays w/ game time and wheel angle at that time */
    private float actionDelay; /* current amount of action delay in seconds */

    /* Variables for Vision Delay */
    private float pathHistory = 1f; /* amount of history of path shown in seconds */
    private float visionDelay; /* current amount of vision delay in seconds */
       
    void Start()
    {
        /* load variables from menu class */
        inputFilePath = BeginButtonControl.inputFilePath;
        RecordFileName = BeginButtonControl.saveFileName;
        sensitivity = 0.01f * BeginButtonControl.sensitivity; 
       

        LoadTestParameters(ref trailPositionCurve, ref actionQuantizationCurve, ref visionDelayCurve, ref bumpSizeCurve, ref actionDelayCurve, ref visionQuantizationCurve);

        /**************************************
         * Initialize Logitech Steering Wheel *
         **************************************/

        /* allow full range of motion for the steering wheel & set steering wheel gains */
        logiIni = LogitechGSDK.LogiSteeringInitialize(false); 
        LogitechGSDK.LogiControllerPropertiesData properties = new LogitechGSDK.LogiControllerPropertiesData();
        wheelPropSetup(ref properties);
        LogitechGSDK.LogiSetPreferredControllerProperties(properties); /* set properties of wheel */
        rec = LogitechGSDK.LogiGetStateCSharp(deviceIdx); 

        /***********************
         * Set Up GUI Elements *
         **********************/

        /* create the horizontal axis line */
        horizontalLine.SetWidth(horizontalLineWidth, horizontalLineWidth);
        horizontalLine.SetVertexCount(2);
        horizontalLine.SetPositions(new Vector3[2] { new Vector3( -11, 0, 0), new Vector3(11, 0, 0) });
        horizontalLine.material = new Material(Shader.Find("Sprites/Default"));
        horizontalLine.SetColors(Color.magenta, Color.magenta); 
        

        /* create path line */
        pathLine.SetWidth(pathLineWidth, pathLineWidth);
        pathLine.SetVertexCount(2);
        pathLine.SetPositions(new Vector3[2] { new Vector3(0, -pathLineHeight, 0), new Vector3(0, pathLineHeight, 0) });
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.SetColors(Color.grey, Color.grey);
        pathHistory = 1f; /* amount of history of path shown in seconds */
        
        /* create vertical line that represents player position */
        playerLine.SetWidth(playerLineWidth, playerLineWidth);
        playerLine.SetVertexCount(2);
        playerLineXPos = 0;
        playerLine.SetPositions(new Vector3[2] { new Vector3(playerLineXPos, -playerLineHeight, 0), new Vector3(playerLineXPos, playerLineHeight, 0) });
        playerLine.material = new Material(Shader.Find("Sprites/Default"));
        playerLine.SetColors(Color.green, Color.green);
        playerLine.sortingOrder = 1;

        /******************************************
         * Set up Parameters for Game Experiments *
         ******************************************/

        saveFilePath = Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));

        gameDate = DateTime.UtcNow.ToString("yyyyMMddhhmmss");
        
        gameIsRunning = true;
      
    }

    void Update()
    {
       
        /* update current in-game time and round to nearest millisecond */
        gameTime = Time.timeSinceLevelLoad;
        gameTime = (float)(Math.Round((double)gameTime, 3));
      

        /* check if game is over */
        if ((int) gameTime > gameLength)
        {
            gameIsRunning = false;
            saveScoreFile(errorData.ToArray());
            SceneManager.LoadScene(4);
        }

        /* if game is not over & still running */
        if (gameIsRunning)
        {

            //horizontalLine.SetPositions(new Vector3[2] { new Vector3(-11, 0, 1), new Vector3(11, 0, 1) });

            /* update action quantization level & notification */
            actionQuantizationLevel = (int) Math.Ceiling( actionQuantizationCurve.Evaluate(gameTime));
            actionQuantizationNotification.text = "Action Quant: " + actionQuantizationLevel;

            /* update vision quantization level & notification */
            visionQuantizationLevel = (int)Math.Ceiling(visionQuantizationCurve.Evaluate(gameTime));
            visionQuantizationNotification.text = "Vision Quant: " + visionQuantizationLevel;

            /* update path line */
            lineUpdate(ref pathLine, visionDelayCurve.Evaluate(gameTime), pathHistory);

            /* update path position variable */
            pathPositionXPos = trailPositionCurve.Evaluate(gameTime);

            /*update time notification*/
            timeNotification.text = (int)gameTime + "s";

            /* update bump size variable */
            bumpSize = bumpSizeCurve.Evaluate(gameTime);

            /* update vision delay notification */
            visionDelay = visionDelayCurve.Evaluate(gameTime);
            visionDelayNotification.text = "Vision Delay: " + visionDelay;

            /* update action delay notification */
            actionDelay = actionDelayCurve.Evaluate(gameTime);
            actionDelayNotification.text = "Action Delay: " + actionDelay;

            /* make sure steering wheel is initialized and connected: */
            if (!logiIni)
            {
                logiIni = LogitechGSDK.LogiSteeringInitialize(false);
            }


            if (!LogitechGSDK.LogiIsConnected(deviceIdx))
            {
                Debug.Log("PLEASE PLUG IN A STEERING WHEEL OR A FORCE FEEDBACK CONTROLLER");

            }

            /********************************** 
             * Wheel is Connected and Working *
             **********************************/

            else if (LogitechGSDK.LogiUpdate())
            {

                /* record state of the wheel and converts to an angle in degrees */
                rec = LogitechGSDK.LogiGetStateCSharp(deviceIdx);
                normalizedAngle = rec.lX * 900 / 65536;

                wheelAngles.Add(new float[] { gameTime, normalizedAngle });
                               
               for(int i = 0; i < wheelAngles.Count; i++)
                {
                 
                    if(gameTime - wheelAngles[i][0] > actionDelay)
                    {
                        quantizedAngle = QuantizeAngle(wheelAngles[i][1], actionQuantizationLevel);
                        wheelAngles.RemoveRange(0, i);
                    }

                }

                          
                /* add to the player position with a scaled factor of the angle of the wheel
                 * NOTE: player position is related to previous position of the player.
                 */
                playerLineXPos = playerLineXPos + sensitivity * quantizedAngle;

                LogitechGSDK.LogiPlayConstantForce(deviceIdx, (int) bumpSize);
              
                /* keep the player position from going off the screen */ 
                if (playerLineXPos < XminScreen)
                {
                    playerLineXPos = XminScreen;
                }
                else if (playerLineXPos > XmaxScreen)
                {
                    playerLineXPos = XmaxScreen;
                }

                /* set position of the player line */
                playerLine.SetPositions(new Vector3[2] { new Vector3(playerLineXPos, -playerLineHeight, 0), new Vector3(playerLineXPos, playerLineHeight, 0) });

                /* calculate the error and add to list */
                reportPositions();
            }

            else
            {
                Debug.Log("THIS WINDOW NEEDS TO BE IN FOREGROUND IN ORDER FOR THE SDK TO WORK PROPERLY... OR TEST ENDED");
            }
        }

    }

    public void lineUpdate(ref LineRenderer lineToUpdate, float visionDelay, float pastHistoryT) 
    {
        /* redraws line each time step
         * inputs are: 
         * pointer to LineRenderer, time of advanced warning, time of past trail history */
        

        float dt = 0.01f;
        int visionQuantLevel = (int) visionQuantizationCurve.Evaluate(gameTime);

        /* how many seconds of the trail will be shown in total (past - visionDelay) 
         *  ie. if vision delay is -2 sec with 1 second of past shown,
         *  total length of line shown is (1 - (-2)) = 3 seconds of trail */

        float tspanLine = (pastHistoryT - visionDelay); 
        int numPoints = (int)(Math.Ceiling(tspanLine / dt)) + 1;
        lineToUpdate.SetVertexCount(numPoints);

        List<Vector3> pathVectors = new List<Vector3>();

        float timePoint;
        float yPoint;
        float xPoint;
        float QuantTWidth;

        /* minimum y-value to start generating trail at
         * product of forward speed and amount of past 
         * history shown in seconds */
        float Ymin = -dy_per_dt * pastHistoryT;

        for(int i = 0; i < numPoints; i++) 
        {
            /* create vector corresponding to path angle 
             * at current time point and add to array */

            timePoint = -pastHistoryT + i * dt;
            yPoint = Ymin + dy_per_dt * dt * i;
            xPoint = QuantizeTrailPosition(trailPositionCurve.Evaluate(gameTime + timePoint), visionQuantLevel);
            pathVectors.Add(new Vector3(xPoint, yPoint, 0f));
            
        }

        /* update path x-position on y-axis at current game time */
        pathLineXPos = trailPositionCurve.Evaluate(gameTime);

        /* draw line with array of points created above */
        QuantTWidth = QuantizeTrailWidth((int)visionQuantizationCurve.Evaluate(gameTime));
        lineToUpdate.SetWidth(QuantTWidth, QuantTWidth);
        
        lineToUpdate.SetPositions(pathVectors.ToArray());
        
    }

    public float QuantizeTrailPosition(float trailPosition, int quantLevel)
    {
        /* quantize horizontal axis */

        int numLevels = (int)Math.Pow(2, quantLevel);
        float deltaX = (float)(XmaxScreen - XminScreen) / ((float)numLevels); 
        if(trailPosition > XmaxScreen)
        {
            return XmaxScreen; 
        }
        if(trailPosition < XminScreen)
        {
            return XminScreen; 
        }
        /*for(float XLeft = XminScreen; XLeft <= XmaxScreen; XLeft+= deltaX)
        {
            
            if(trailPosition < XLeft)
            {
                return (XLeft + (XLeft - deltaX)) / 2; 
            }
        }*/
        
        int leftBlock = (int)Math.Floor((trailPosition - XminScreen) / deltaX);
        if (leftBlock == numLevels) { leftBlock = numLevels - 1; } // not go out of the screen
        return XminScreen + leftBlock * deltaX + deltaX / 2f;
    }

    public float QuantizeTrailWidth(int quantLevel)
    {
        int numLevels = (int)Math.Pow(2, quantLevel);
        float width = (float)(XmaxScreen - XminScreen) / (float)numLevels;
        if (width < 0.1f)
        {
            return 0.1f;
        }
        else if (width > 1f)
        {
            return 1f;
        }
        return width;  
    }
    public float QuantizeAngle(float wheelAngle, int quantLevel)
    {

        /* quantization level is the number of bits available for quantization of input */
        int numLevels = (int) Math.Pow(2, quantLevel);
        float angleDifference = totalAngleRange / numLevels;

        /* if wheelAngle is greater than largest angle in range
         * return max value of angle range */
        if (wheelAngle > totalAngleRange / 2)
        {
            return totalAngleRange / 2;
        }
        else if (wheelAngle < -totalAngleRange / 2)
        {
            return -totalAngleRange / 2;
        }
        /* else look which quantized angle value the wheel angle is closest too */
        else
        {
            /* loop through quantized levels */
            for (float quantizedAngle = 0; quantizedAngle <= (totalAngleRange / 2); quantizedAngle += angleDifference)
            {
                /* skip 0 because can't add 0 as possible quanitize level */
                if (quantizedAngle != 0)
                {
                    if (Math.Abs(wheelAngle) <= quantizedAngle)
                    {
                        /* return negative of quantized angle
                         * if wheel angle is negative */
                        if (wheelAngle < 0)
                        {
                            return quantizedAngle * -1;
                        }
                        else
                            return quantizedAngle; 
                      
                    }
                }
            }
        }

        /* this option should never run */
        return 0;
    }


    public void LoadTestParameters(ref AnimationCurve path, ref AnimationCurve actQuant, ref AnimationCurve visionDelay, ref AnimationCurve bump, ref AnimationCurve actDelay, ref AnimationCurve visQuant)
    {
        path = new AnimationCurve();
        path.preWrapMode = WrapMode.ClampForever;
        path.postWrapMode = WrapMode.ClampForever;
        path.AddKey(0, 0);
       

        actQuant = new AnimationCurve();
        actQuant.preWrapMode = WrapMode.ClampForever;
        actQuant.postWrapMode = WrapMode.ClampForever;
        actQuant.AddKey(0, 0);

        visionDelay = new AnimationCurve();
        visionDelay.preWrapMode = WrapMode.ClampForever;
        visionDelay.postWrapMode = WrapMode.ClampForever;
        visionDelay.AddKey(0, 0);

        bump = new AnimationCurve();
        bump.preWrapMode = WrapMode.ClampForever;
        bump.postWrapMode = WrapMode.ClampForever;
        bump.AddKey(0, 0);

        actDelay = new AnimationCurve();
        actDelay.preWrapMode = WrapMode.ClampForever;
        actDelay.postWrapMode = WrapMode.ClampForever;
        actDelay.AddKey(0, 0);

        visQuant = new AnimationCurve();
        visQuant.preWrapMode = WrapMode.ClampForever;
        visQuant.postWrapMode = WrapMode.ClampForever;
        visQuant.AddKey(0, 0);

        try
        {
            string line;
            StreamReader theReader = new StreamReader(inputFilePath, System.Text.Encoding.Default);

            using (theReader)
            {

                float time;
                float pathVal;
                float actQuantVal;
                float visVal;
                float bumpVal;
                float actVal;
                float visQuantVal; 
               
                do
                {
                    
                    line = theReader.ReadLine();

                    if (line != null)
                    {
                        string[] entries = line.Split(',');
                       

                        if (entries.Length > 0)
                        {
                            time = Convert.ToSingle(entries[0]);
                            pathVal = Convert.ToSingle(entries[1]);
                            bumpVal = Convert.ToSingle(entries[2]);
                            actQuantVal = Convert.ToSingle(entries[3]);
                            actVal = Convert.ToSingle(entries[4]);
                            visVal = Convert.ToSingle(entries[5]);
                            visQuantVal = Convert.ToSingle(entries[6]);

                            path.AddKey(time, pathVal);
                            actQuant.AddKey(time, actQuantVal);
                            visionDelay.AddKey(time, visVal);
                            bump.AddKey(time, bumpVal);
                            actDelay.AddKey(time, actVal);
                            visQuant.AddKey(time, visQuantVal);

                            gameLength = (int)time + 1; 
                            
                        }
                    }
                }
                while (line != null);
               
                theReader.Close();
                return;
            }
        }
        /* If anything broke in the try block, we throw an exception with information
         * on what didn't work */
        catch (Exception e)
        {
            Debug.Log("ERROR: " +  e.Message);
            return;
        }

        
    }

    public void wheelPropSetup(ref LogitechGSDK.LogiControllerPropertiesData prop)
    {
        /* set up wheel to allow full range of motion and set gains
         * input is pointer to struct to hold wheel properties */

        prop.wheelRange = 900;

        /* set gains for the steering wheel: */
        prop.forceEnable = true;
        prop.overallGain = 80;
        prop.springGain = 80;

    }

    public void reportPositions()
    {
        /* Method responsible for
         * saving current values of
         * data necesary for error 
         * calculation to an array
         * so they can be saved to a
         * text file later
         * 
         * data saved in order of:
         * time, trail position, bump size, quantization level, action delay, vision delay, position error, angle of wheel */
         
       

        float time = gameTime;
        float trailPosition = pathPositionXPos;
        float bump = bumpSize;
        float actQuantLevel = actionQuantizationLevel;
        float actDelay = actionDelay;
        float visDelay = visionDelay; 
        float playerError = playerLineXPos - pathPositionXPos;
        float angle = normalizedAngle;
        float visQuantLevel = visionQuantizationLevel; 
        
       

        errorData.Add(
                  time +
            "," + trailPosition +
            "," + bump +
            "," + actQuantLevel +
            "," + actDelay +
            "," + visDelay + 
            "," + visQuantLevel + 
            "," + playerError + 
            "," + angle);

    }

    public void saveScoreFile(string[] data)
    {
        /* Method responsible for outputting 
        * saved positions and data for error
        * calculation later. Data to be written
        * in the order of:
        * time, player position, width, distance (Goal Position)
        * 
        * FILES SAVED IN UnityCode/FittsLawData
        */


        string outputFilePath = saveFilePath + "/MountainBikeData/" + RecordFileName + "_" + gameDate + ".txt";

        /* Save Score: */
        if (!File.Exists(outputFilePath)) /* If it doesn't exist yet, create it */
        {
            File.WriteAllText(outputFilePath, "");
        }
        File.WriteAllLines(outputFilePath, data);
        

    }
}