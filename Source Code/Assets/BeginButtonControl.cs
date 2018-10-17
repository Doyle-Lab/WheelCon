using UnityEngine;
using UnityEngine.UI;
using SFB;
using System; 
 

public class BeginButtonControl : MonoBehaviour {

    public static string saveFileName;
    public static string inputFilePath;
    public static float sensitivity; 

    public InputField saveFileField;
    public Slider sensitivitySlider;
    public Button thisButton;

    public void setFileName()
    {
       
        saveFileName = saveFileField.text;
    }

    public void setInputFilePath()
    {

        inputFilePath = StandaloneFileBrowser.OpenFilePanel("Select Input File", "", "txt", false)[0];
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (saveFileField != null && thisButton != null)
        {
            setFileName();

            if (saveFileName != "" && inputFilePath != null)
            {
                thisButton.interactable = true;
            }
            else
                thisButton.interactable = false;
        }
    }


    public void setSensitivity()
    {
        sensitivity = sensitivitySlider.value; 
    }
}
