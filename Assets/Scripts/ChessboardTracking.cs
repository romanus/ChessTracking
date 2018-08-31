using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;
using NWH;
using System.Runtime.InteropServices;

public class ChessboardTracking : MonoBehaviour {

	public RawImage rawImage;
	private WebCamTexture webCamTexture;
	private Texture2D tex;
	private Mat mat, gray;

	private Size size = new Size(9, 6);
	private float cellSize = 0.026f; // in metres

	void Start()
	{
		webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name);
		webCamTexture.Play();

		tex = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
		mat = new Mat(webCamTexture.height, webCamTexture.width, MatType.CV_8UC4);
        gray = new Mat(webCamTexture.height, webCamTexture.width, MatType.CV_8UC1);
	}

	void Update()
	{
        if (webCamTexture.didUpdateThisFrame && webCamTexture.isPlaying && rawImage != null)
		{
			CamUpdate();
		}
	}

	void CamUpdate()
	{
		CvUtil.GetWebCamMat(webCamTexture, ref mat);

        Cv2.CvtColor(mat, gray, ColorConversionCodes.RGBA2GRAY);

		Point2f[] corners;

		bool ret = Cv2.FindChessboardCorners(gray, size, out corners);
		
		if(ret){

			TermCriteria criteria = TermCriteria.Both(30, 0.001f);
			Point2f[] corners2 = Cv2.CornerSubPix(gray, corners, size, new Size(-1,-1), criteria);

			Cv2.DrawChessboardCorners(mat, size, corners2, ret);

			List<Point3f> lObjectPoints = new List<Point3f>();
			for(int i=0; i<size.Width; i++)
				for(int j=0; j<size.Height; j++)
					lObjectPoints.Add(new Point3f(i,j,0) * cellSize);
			var objectPoints = new List<IEnumerable<Point3f>>{ lObjectPoints };

			var imagePoints = new List<IEnumerable<Point2f>>{ corners2 };

			double[,] cameraMatrix = new double[3,3];
			double[] distCoefficients = new double[5];
			Vec3d[] rvecs, tvecs;

			Cv2.CalibrateCamera(objectPoints, imagePoints, mat.Size(), cameraMatrix, distCoefficients, out rvecs, out tvecs);
			
			print(
				cameraMatrix[0,0] + ", " + cameraMatrix[0,1] + ", " + cameraMatrix[0,2] + "\n" + 
				cameraMatrix[1,0] + ", " + cameraMatrix[1,1] + ", " + cameraMatrix[1,2] + "\n" + 
				cameraMatrix[2,0] + ", " + cameraMatrix[2,1] + ", " + cameraMatrix[2,2]
			);

			print(tvecs[0].Item0 + ", " + tvecs[0].Item1 + ", " + tvecs[0].Item2);

		}

		CvConvert.MatToTexture2D(mat, ref tex);
		rawImage.texture = tex;
	}

	private void OnDestroy()
	{
		webCamTexture.Stop();
	}
}

public static class Ext{
	public static IEnumerable<IEnumerable<T>> To2DIEnumerable<T>(this T[,] array)
    {
        if (array == null) return null;
        List<IEnumerable<T>> mainList = new List<IEnumerable<T>>();
        for (int i = 0; i < array.GetLength(0); i++)
        {
            List<T> list = new List<T>();
			for(int j=0; j<array.GetLength(1); j++)
				list.Add(array[i,j]);

            mainList.Add(list);
        }
        return mainList;
    }
}
