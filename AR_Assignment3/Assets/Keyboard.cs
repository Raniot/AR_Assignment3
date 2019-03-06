using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using UnityEngine;
using Vuforia;

public class Keyboard : MonoBehaviour
{
    private Mat cameraImageMat;
    public Transform FingerPlane;
    public TextMesh Text;

    public Camera Camera;
    // Start is called before the first frame update
    void Start()
    {
        Text.text = "This is a test";
    }

    // Update is called once per frame
    void Update()
    {
        MatDisplay.SetCameraFoV(41.5f);

        Image cameraImage = CameraDevice.Instance.GetCameraImage(Image.PIXEL_FORMAT.RGBA8888);

        if (cameraImage != null)
        {
            if (cameraImageMat == null)
            {
                //First frame -> generate Mat with same dimensions as camera feed
                cameraImageMat = new Mat(cameraImage.Height, cameraImage.Width, CvType.CV_8UC4);
            }
            cameraImageMat.put(0, 0, cameraImage.Pixels); // transferring image data to Mat

            var fingerColorMat = FindFingerColor();
            try
            {
                var fingerTip = FindFingerTip(fingerColorMat);
                var screenToWorldPoint = Camera.ScreenToWorldPoint(fingerTip);
                screenToWorldPoint.y = -screenToWorldPoint.y;
                FingerPlane.position = screenToWorldPoint;
            }
            catch {}
            



            MatDisplay.DisplayMat(cameraImageMat, MatDisplaySettings.FULL_BACKGROUND);
        }




        if (Input.GetKeyDown(KeyCode.Space))
            Text.text = Text.text.Remove(Text.text.Length - 1);
    }

    private Mat FindFingerColor()
    {
        var frameHsv = new Mat();
        // Convert from BGR to HSV colorspace
        Imgproc.cvtColor(cameraImageMat, frameHsv, Imgproc.COLOR_RGB2HSV);
        // Detect the object based on HSV Range Values
        var frameThreshold = new Mat();
        Core.inRange(frameHsv, new Scalar(0, 30, 60), new Scalar(20, 150, 255), frameThreshold);

        var kernelSize = Mat.ones(20, 20, CvType.CV_8U);
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
                    return new Vector3(j, i, 1);
            }
        }
        throw new Exception("Finger tip not found");
    }
}
