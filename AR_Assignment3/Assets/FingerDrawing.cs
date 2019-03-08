using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using UnityEngine;
using Vuforia;

public class FingerDrawing : MonoBehaviour
{

    private Mat _cameraImageMat;
    public Camera Cam;
    public Transform Corner1;
    public Transform Corner2;
    public Transform Corner3;
    public Transform Corner4;
    public Transform Draw;
    public Transform Color;
    public Transform FingerPlane;
    public Transform FingerPointTarget;

    private Mat _texture;
    private MatOfPoint2f _imagePoints;


    // Start is called before the first frame update
    void Start()
    {
        //Anders magic number
        // _texture = new Mat(300,200,CvType.CV_8UC4);
        //_texture.setTo(new Scalar(100, 200, 0));
        _texture = MatDisplay.LoadRGBATexture("muscle_tex.png");
        _imagePoints = new MatOfPoint2f();
        _imagePoints.alloc(4);
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

        if (FingerPointTarget.GetComponent<ImageTargetBehaviour>().CurrentStatus != TrackableBehaviour.Status.TRACKED)
        {
            MatDisplay.DisplayMat(_cameraImageMat, MatDisplaySettings.FULL_BACKGROUND);
            return;
        }

        var corner1ScreenPoint = Cam.WorldToScreenPoint(Corner1.position);
        var corner2ScreenPoint = Cam.WorldToScreenPoint(Corner2.position);
        var corner3ScreenPoint = Cam.WorldToScreenPoint(Corner3.position);
        var corner4ScreenPoint = Cam.WorldToScreenPoint(Corner4.position);

        Vector2 uv1 = new Vector2(corner1ScreenPoint.x, corner1ScreenPoint.y) / corner1ScreenPoint.z;
        Vector2 uv2 = new Vector2(corner2ScreenPoint.x, corner2ScreenPoint.y) / corner2ScreenPoint.z;
        Vector2 uv3 = new Vector2(corner3ScreenPoint.x, corner3ScreenPoint.y) / corner3ScreenPoint.z;
        Vector2 uv4 = new Vector2(corner4ScreenPoint.x, corner4ScreenPoint.y) / corner4ScreenPoint.z;

        //Do not forget to alloc before putting values into a MatOfPoint2f (see Start() above)
        //We need to flip the v-coordinates, see coordinate system overview
        float maxV = cameraImage.Height - 1;
        _imagePoints.put(0, 0, uv1.x, maxV - uv1.y);
        _imagePoints.put(1, 0, uv2.x, maxV - uv2.y);
        _imagePoints.put(2, 0, uv3.x, maxV - uv3.y);
        _imagePoints.put(3, 0, uv4.x, maxV - uv4.y);

        //Debug draw points using OpenCV's drawing functions
        Point imgPnt1 = new Point(_imagePoints.get(0, 0));
        Point imgPnt2 = new Point(_imagePoints.get(1, 0));
        Point imgPnt3 = new Point(_imagePoints.get(2, 0));
        Point imgPnt4 = new Point(_imagePoints.get(3, 0));
        var srcPoints = new List<Point> { imgPnt2, imgPnt1, imgPnt4, imgPnt3 };
        var dstPoints = new List<Point>
        {
            new Point(0, _texture.height()),
            new Point(_texture.width(), _texture.height()),
            new Point(_texture.width(), 0),
            new Point(0, 0),
        };

        var matObj = new MatOfPoint2f(srcPoints.ToArray());
        var matDst = new MatOfPoint2f(dstPoints.ToArray());
        var H = Calib3d.findHomography(matObj, matDst);
/*
        try
        {
            var fingerColorMat = FindFingerColor();
            var fingerPointInWorldSpace = FingerPointInWorldSpace(fingerColorMat);
            FingerPlane.position = fingerPointInWorldSpace;


            var colorPixelValue = FindPixelValue(_cameraImageMat, Color.position);
            var drawPixelValue = FindPixelValue(fingerColorMat, Draw.position);

            if ((int)drawPixelValue[0] == 255)
            {
                Debug.Log($"{colorPixelValue[0]}, {colorPixelValue[1]}, {colorPixelValue[2]}");
                Debug.Log("Found Draw");
                //draw at finger pos
                var fingerScreenPoint = Cam.WorldToScreenPoint(fingerPointInWorldSpace);
                _texture.put((int)fingerScreenPoint.x, (int)fingerScreenPoint.y, colorPixelValue);
            }
        }
        catch
        {
        }
        */
        
        var warpedMat = new Mat();
        Imgproc.warpPerspective(_texture, warpedMat, H.inv(), _cameraImageMat.size(),
            Imgproc.INTER_LINEAR);
        warpedMat.convertTo(warpedMat, _cameraImageMat.type());

        var blendTex = new Mat();
        Core.addWeighted(_cameraImageMat, 0.95f, warpedMat, 0.4f, 0.0, blendTex);

        MatDisplay.DisplayMat(blendTex, MatDisplaySettings.FULL_BACKGROUND);
    }

    private double[] FindPixelValue(Mat mat, Vector3 pos)
    {
        pos.y = -pos.y;
        var ScreenPoint = Cam.WorldToScreenPoint(pos);
        return mat.get((int) ScreenPoint.y, (int) ScreenPoint.x);
    }

    private Vector3 FingerPointInWorldSpace(Mat fingerColorMat)
    {
        var fingerTip = FindFingerTip(fingerColorMat);
        var screenToWorldPoint = Cam.ScreenToWorldPoint(fingerTip);
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
        Core.inRange(frameHsv, new Scalar(0, 30, 60), new Scalar(20, 100, 255), frameThreshold);

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
                if ((int)value[0] == 255)
                    return new Vector3(j, i, 2);
            }
        }
        throw new Exception("Finger tip not found");
    }
}
