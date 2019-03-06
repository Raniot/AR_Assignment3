using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class colorScript : MonoBehaviour
{
    public GameObject Red;
    public GameObject Blue;
    public GameObject Green;
    public GameObject Contrast;

    private ImageTargetBehaviour _redImageTargetBehaviour;
    private ImageTargetBehaviour _blueImageTargetBehaviour;
    private ImageTargetBehaviour _greenImageTargetBehaviour;

    // Start is called before the first frame update
    void Start()
    {
        _redImageTargetBehaviour = Red.GetComponent<ImageTargetBehaviour>();
        _blueImageTargetBehaviour = Blue.GetComponent<ImageTargetBehaviour>();
        _greenImageTargetBehaviour = Green.GetComponent<ImageTargetBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        var redState = _redImageTargetBehaviour.CurrentStatus == TrackableBehaviour.Status.TRACKED;
        var blueState = _blueImageTargetBehaviour.CurrentStatus == TrackableBehaviour.Status.TRACKED;
        var greenState = _greenImageTargetBehaviour.CurrentStatus == TrackableBehaviour.Status.TRACKED;

        var yaw = Contrast?.transform.eulerAngles.y/360 ?? 1;



        SetColor(redState, blueState, greenState, yaw);
    }

    private void SetColor(bool redState, bool blueState, bool greenState, float yaw)
    {
        if (redState && blueState && greenState)
            transform.GetComponent<Renderer>().material.color = Color.white;
        else if (!redState && !blueState && !greenState)
            transform.GetComponent<Renderer>().material.color = Color.black;
        else if (redState && blueState)
            transform.GetComponent<Renderer>().material.color = Color.magenta;
        else if (redState && greenState)
            transform.GetComponent<Renderer>().material.color = Color.yellow;
        else if (blueState && greenState)
            transform.GetComponent<Renderer>().material.color = Color.cyan;
        else if (redState)
            transform.GetComponent<Renderer>().material.color = Color.red;
        else if (blueState)
            transform.GetComponent<Renderer>().material.color = Color.blue;
        else if (greenState)
            transform.GetComponent<Renderer>().material.color = Color.green;

        Color.RGBToHSV(transform.GetComponent<Renderer>().material.color, out var h, out var s, out var v);
        v = yaw;
        transform.GetComponent<Renderer>().material.color = Color.HSVToRGB(h, s, v);

    }
}
