using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerFader : MonoBehaviour
{
    //[Tooltip("The fader component that will be effected")]
    //public TransitionalFader fader;
    [Tooltip("Disables the gameoject after use")]
    public bool disableOnTrigger = false;
    [Tooltip("Amount of time spent fading in")]
    public float secondsIn = 1;
    [Tooltip("Amount of time spent staying faded")]
    public float secondsStay = 1;
    [Tooltip("Amount of time spent fading out")]
    public float secondsOut = 1;

    public string playerTag = "Player";

    //Fades the screen
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != playerTag) return;
        //fader.FadeScreen(secondsIn, secondsStay, secondsOut);
        if (disableOnTrigger) gameObject.SetActive(false);
    }
}
