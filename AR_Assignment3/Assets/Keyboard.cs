using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using UnityEngine;
using Vuforia;

public class Keyboard : MonoBehaviour
{
    private Mat _cameraImageMat;
    public Transform FingerPlane;
    public Transform AlphabetTarget;
    public List<Transform> KeyboardPos;
    public Transform Send;
    private readonly WaitForSeconds _typingDelay = new WaitForSeconds(0.75f);


    public TextMesh Text;

    public Camera Camera;

    private bool _keyPressed;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 28; i++)
        {
            KeyboardPos.Add(transform.GetChild(i));
        }
    }

    // Update is called once per frame
    void Update()
    {
        MatDisplay.SetCameraFoV(41.5f);

        Image cameraImage = CameraDevice.Instance.GetCameraImage(Image.PIXEL_FORMAT.RGBA8888);

        if (cameraImage == null) return;
        if (_cameraImageMat == null)
        {
            //First frame -> generate Mat with same dimensions as camera feed
            _cameraImageMat = new Mat(cameraImage.Height, cameraImage.Width, CvType.CV_8UC4);
        }
        _cameraImageMat.put(0, 0, cameraImage.Pixels); // transferring image data to Mat

        if (AlphabetTarget.GetComponent<ImageTargetBehaviour>().CurrentStatus != TrackableBehaviour.Status.TRACKED)
        {
            MatDisplay.DisplayMat(_cameraImageMat, MatDisplaySettings.FULL_BACKGROUND);
            return;
        }
        var fingerColorMat = FindFingerColor();
        var test = Send.position;
        test.y = -test.y;
        var screenPosSend = Camera.WorldToScreenPoint(test);
        var value = fingerColorMat.get((int)screenPosSend.y, (int)screenPosSend.x);

        //Debug.Log($"Circle: {screenPosSend}");
        //Debug.Log($"backspace: {Camera.WorldToScreenPoint(KeyboardPos[27].position)}");
        //Debug.Log($"backspace: {Camera.WorldToScreenPoint(KeyboardPos[20].position)}");

        //Debug.Log($"{value[0]}");

        // Check if value at finger is white (which means that finger is present)

        try
        {
            var fingerPointInWorldSpace = FingerPointInWorldSpace(fingerColorMat);
            FingerPlane.position = fingerPointInWorldSpace;

            if ((int)value[0] > 250 && !_keyPressed)
            {
                StartCoroutine(DelayTyping());
                var oldDistance = float.MaxValue;
                var letter = string.Empty;

                var maxDistance = Vector3.Distance(Camera.WorldToScreenPoint(KeyboardPos[0].position),
                    Camera.WorldToScreenPoint(KeyboardPos[7].position));

                KeyboardPos.ForEach(x =>
                {
                    var worldToScreenPoint = Camera.WorldToScreenPoint(x.position);
                    var distance = Vector3.Distance(Camera.WorldToScreenPoint(fingerPointInWorldSpace),
                        worldToScreenPoint);
                    if (distance > oldDistance || distance > maxDistance) return;
                    letter = x.name;
                    oldDistance = distance;

                });
                Debug.Log(letter);
                switch (letter)
                {
                    case "BackSpace":
                        Text.text = Text.text.Remove(Text.text.Length - 1);
                        break;
                    case "Space":
                        Text.text += " ";
                        break;
                    default:
                        Text.text += letter;
                        break;
                }
            }
        }
        catch
        {
        }
        
        MatDisplay.DisplayMat(_cameraImageMat, MatDisplaySettings.FULL_BACKGROUND);

    }

    private Vector3 FingerPointInWorldSpace(Mat fingerColorMat)
    {
        var fingerTip = FindFingerTip(fingerColorMat);
        var screenToWorldPoint = Camera.ScreenToWorldPoint(fingerTip);
        screenToWorldPoint.y = -screenToWorldPoint.y;
        return screenToWorldPoint;
    }

    private Mat FindFingerColor()
    {
        var frameHsv = new Mat();
        // Convert from BGR to HSV colorspace
        Imgproc.cvtColor(_cameraImageMat, frameHsv, Imgproc.COLOR_RGB2HSV);
        // Detect the object based on HSV Range Values
        var frameThreshold = new Mat();
        Core.inRange(frameHsv, new Scalar(0, 30, 60), new Scalar(20, 150, 255), frameThreshold);

        var kernelSize = Mat.ones(10, 10, CvType.CV_8U);
        var frameMorphClose = new Mat();
        Imgproc.morphologyEx(frameThreshold, frameMorphClose, Imgproc.MORPH_CLOSE, kernelSize);
        var frameMorphOpen = new Mat();
        Imgproc.morphologyEx(frameMorphClose, frameMorphOpen, Imgproc.MORPH_OPEN, kernelSize);
        return frameMorphOpen;
    }

    private Vector3 FindFingerTip(Mat fingerColorMat)
    {
        for (int i = 0; i < fingerColorMat.height(); i++)
        {
            for (int j = 0; j < fingerColorMat.width(); j++)
            {
                var value = fingerColorMat.get(i, j);
                if ((int) value[0] == 255)
                    return new Vector3(j, i, 2);
            }
        }
        throw new Exception("Finger tip not found");
    }


    private IEnumerator DelayTyping()
    {
        _keyPressed = true;
        yield return _typingDelay;
        _keyPressed = false;

    }
}
