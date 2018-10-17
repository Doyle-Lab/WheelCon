using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ValSensitivity : MonoBehaviour {

    public InputField field;
    public Slider slider;
	public void setField()
    {
        field.text = slider.value.ToString("0.000");
    }

    public void setSlider()
    {
        slider.value = Convert.ToSingle(field.text); 
    }

}
