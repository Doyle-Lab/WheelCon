using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class FittsLawTask : MonoBehaviour
{
    /**************************
     * FOR TOOLBOX PUBLISHING *
     *************************/

    /***********************************************
     *           Game Version for the              *
     *        Verification of Fitts Law            *
     *                                             *
     * Input File Format:                          *
     * Time, Goal Width, Goal Distance             *
     *                                             *     
     * Results Stored as:                          *
     * Time, Player Position, Goal Width, Distance *
     **********************************************/


    /* Variables for File Interaction */
    private static string inputFilePath; /* path to input file */
    private string saveFilePath; /* path to output file */
    private List<string> errorData; /* holds error information of each cycle in a formatted string */
    private static string RecordFileName; /* name of error-output file */
    private string gameDate; /* current game date to append to file name */

    /* Horizontal Axis */
    public LineRenderer horizontalLine;

    /* player line of constant width and height but variable x-position */
    public LineRenderer playerLine;
    private float playerLineHeight = 2;
    private float playerLineXPos = 0;

    /* goal line of variable width and x-position, but constant height */
    public LineRenderer goalLine;
    private int goalLineHeight;
    private float goalLineWidth;
    private float goalLineXPos;


    /* Variables for Interacting with Screen Position */
    private float sensitivity; /* sensitivity constant of wheel */
    private const float XminScreen = -10f;
    private const float XmaxScreen = 10f;
    private const float YminScreen = -3.5f;
    private const float YmaxScreen = 6.5f;
    private const float totalAngleRange = 120f; /* accepted angle span of wheel */

    /* Variables for Keeping Time */
    private float gameTime;
    private Boolean gameIsRunning;

    /* Variables for Accessing and Controlling the Wheel */
    private int deviceIdx = 0; /* corresponds to first wheel plugged into computer */
    private string wheelProp; /* use to display properties of system */
    private bool logiIni; /* use to check if wheel is working */
    private float NormalizedAngle = 0; /* angle of wheel in degrees */
    private LogitechGSDK.DIJOYSTATE2ENGINES rec; /* current state of the wheel */

    /* Animation Curves for Holding Distance and Width Data of Goal Zone */
    private AnimationCurve distances; /* holds distance of target zone from center */
    private AnimationCurve widths; /* holds width of target zone */

    /* Text Notification of Time */
    public Text timeNotification;

    /* Experimental Parameters */
    private int gameLength;  /* gameLength in seconds */

    void Start()
    {

        /* get file interaction & sensitivity values from menu class */
        RecordFileName = BeginButtonControl.saveFileName;
        inputFilePath = BeginButtonControl.inputFilePath;
        sensitivity = 0.1f * ValSensitivity.sensitivity;


        LoadTestParameters(ref distances, ref widths);

        /**************************************
         * Initialize Logitech Steering Wheel *
         **************************************/

        /* allow full range of motion for the steering wheel & set steering wheel gains */
        wheelProp = "";
        logiIni = LogitechGSDK.LogiSteeringInitialize(false);
        LogitechGSDK.LogiControllerPropertiesData properties = new LogitechGSDK.LogiControllerPropertiesData();
        wheelPropSetup(ref properties);
        LogitechGSDK.LogiSetPreferredControllerProperties(properties);
        rec = LogitechGSDK.LogiGetStateCSharp(deviceIdx);  /*responsible for recording state of the wheel*/


        /* create the horizontal axis line */
        horizontalLine.SetWidth(0.1f, 0.1f);
        horizontalLine.SetVertexCount(2);
        horizontalLine.SetPositions(new Vector3[2] { new Vector3(-11, 0, 0), new Vector3(11, 0, 0) });
        horizontalLine.material = new Material(Shader.Find("Sprites/Default"));
        horizontalLine.SetColors(Color.magenta, Color.magenta);


        /* create vertical line that represents player position */
        playerLine.SetWidth(0.05f, 0.05f);
        playerLine.SetVertexCount(2);
        playerLineXPos = 0;
        playerLine.SetPositions(new Vector3[2] { new Vector3(playerLineXPos, -playerLineHeight, 0), new Vector3(playerLineXPos, playerLineHeight, 0) });
        playerLine.material = new Material(Shader.Find("Sprites/Default"));
        playerLine.SetColors(Color.green, Color.green);
        playerLine.sortingOrder = 2;

        /*create fat line that represent goal zone*/
        goalLineWidth = widths.Evaluate(0.1f);
        goalLine.SetWidth(goalLineWidth, goalLineWidth);
        goalLine.SetVertexCount(2);
        goalLineHeight = 10;
        goalLineXPos = distances.Evaluate(0.1f);
        goalLine.SetPositions(new Vector3[2] { new Vector3(goalLineXPos, -goalLineHeight, 0), new Vector3(goalLineXPos, goalLineHeight, 0) });
        goalLine.material = new Material(Shader.Find("Sprites/Default"));
        goalLine.SetColors(Color.gray, Color.gray);
        goalLine.sortingOrder = 1;


        errorData = new List<string>();

        DateTime nowDate = DateTime.UtcNow;
        gameDate = nowDate.ToString("yyyyMMddhhmmss");


        /******************************************
         * Set up Parameters for Game Experiments *
         ******************************************/
        saveFilePath = Path.GetDirectoryName(Application.dataPath);
        gameIsRunning = true;

    }

    void Update()
    {

        /* update current in-game time and round to nearest millisecond */
        gameTime = Time.timeSinceLevelLoad;
        gameTime = (float)(Math.Round((double)gameTime, 3));


        /* check if game is over
         * if so, write output file & open end game scene */
        if ((int)gameTime > gameLength)
        {
            gameIsRunning = false;
            saveScoreFile(errorData.ToArray());
            SceneManager.LoadScene(5);
        }

        /* if game is not over & still running */
        if (gameIsRunning)
        {

            /* update time notification */
            timeNotification.text = (int)gameTime + "s";

            /* update width of goal zone with info already loaded from text file */
            goalLineWidth = widths.Evaluate(gameTime);
            goalLine.SetWidth(goalLineWidth, goalLineWidth);
            goalLineXPos = distances.Evaluate(gameTime);

            goalLine.SetPositions(new Vector3[2] { new Vector3(goalLineXPos, -goalLineHeight, 0), new Vector3(goalLineXPos, goalLineHeight, 0) });


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
                NormalizedAngle = rec.lX * 900 / 65536;

                /* set the player position to a scaled factor of the angle of the wheel
                 * NOTE: player position is not related to previous position of the player.
                 * this means angle of the wheel = position on the screen
                 */

                playerLineXPos = sensitivity * NormalizedAngle;

                /* keep the player position from going off the screen */
                if (playerLineXPos < XminScreen)
                {
                    playerLineXPos = (float)XminScreen;
                }
                else if (playerLineXPos > XmaxScreen)
                {
                    playerLineXPos = (float)XmaxScreen;
                }

                /* set position of the player line */
                playerLine.SetPositions(new Vector3[2] { new Vector3(playerLineXPos, -playerLineHeight, 0), new Vector3(playerLineXPos, playerLineHeight, 0) });

                /* calculate the error and add to list for output */
                reportPositions();
            }

            else
            {
                Debug.Log("THIS WINDOW NEEDS TO BE IN FOREGROUND IN ORDER FOR THE SDK TO WORK PROPERLY... OR TEST ENDED");
            }
        }

    }


    public void LoadTestParameters(ref AnimationCurve distance, ref AnimationCurve width)
    {
        distance = new AnimationCurve();
        distance.preWrapMode = WrapMode.ClampForever;
        distance.postWrapMode = WrapMode.ClampForever;
        distance.AddKey(0, -1);


        width = new AnimationCurve();
        width.preWrapMode = WrapMode.ClampForever;
        width.postWrapMode = WrapMode.ClampForever;
        width.AddKey(0, -1);

        try
        {
            string line;

            StreamReader theReader = new StreamReader(inputFilePath, System.Text.Encoding.Default);

            using (theReader)
            {

                float time;
                float distanceVal;
                float widthVal;


                do
                {

                    line = theReader.ReadLine();

                    if (line != null)
                    {
                        string[] entries = line.Split(',');


                        if (entries.Length > 0)
                        {
                            time = Convert.ToSingle(entries[0]);
                            widthVal = Convert.ToSingle(entries[1]);
                            distanceVal = Convert.ToSingle(entries[2]);

                            width.AddKey(time, widthVal);
                            distance.AddKey(time, distanceVal);

                            gameLength = (int)time + 1;

                        }
                    }
                }
                while (line != null);
                /* Done reading, close the reader and return true to broadcast success */
                theReader.Close();
                return;
            }
        }
        /* If anything broke in the try block, we throw an exception with information
         * on what didn't work */
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
            return;
        }


    }

    public void wheelPropSetup(ref LogitechGSDK.LogiControllerPropertiesData prop)
    {
        /* set up wheel to allow full range of motion and set gains
         * input is pointer to struct to hold wheel properties */

        /* allow full range of motion */
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
         * time, player position, width, distance
         */


        float time = gameTime;
        float playerPosition = playerLineXPos;
        float width = goalLineWidth;
        float distance = goalLineXPos;

        errorData.Add(
                  time +
            "," + playerPosition +
            "," + width +
            "," + distance);

    }

    public void saveScoreFile(string[] data)
    {
        /* Method responsible for outputting 
        * saved positions and data for error
        * calculation later. Data to be written
        * in the order of:
        * time, player position, width, distance (Goal Position)
        * 
        * FILES SAVED IN "Executable & Output Files/FittsLawData"
        */

        string outputFileName = RecordFileName + "_" + gameDate + ".txt";
        string outputFilePath = saveFilePath + "/FittsLawData/";


        /* check if directory exists & create if not */
        if (!Directory.Exists(outputFilePath))
        {
            Directory.CreateDirectory(outputFilePath);
        }

        /* Save Score: */
        outputFilePath += outputFileName;

        /* If file doesn't exist yet, create it */
        if (!File.Exists(outputFilePath))
        {
            File.WriteAllText(outputFilePath, "");
        }
        File.WriteAllLines(outputFilePath, data);

    }
}