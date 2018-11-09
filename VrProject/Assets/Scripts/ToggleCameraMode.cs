using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ToggleCameraMode : MonoBehaviour {
    static bool toggleSwitch = false; // Start off not in camera mode

    public void SwitchChange()
    {
        toggleSwitch = gameObject.GetComponent<Toggle>().isOn;
    }

	public void SetCameraMode() {
        gameObject.SetActive(toggleSwitch); //!gameObject.activeSelf
	}

    public void Start() {
       // SwitchChange();
        SetCameraMode();
    }

}
