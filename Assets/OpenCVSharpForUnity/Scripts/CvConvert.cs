using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using OpenCvSharp;
using System.Runtime.InteropServices;

namespace NWH
{
    public class CvConvert
    {

        /// <summary>
        /// Converts Unity Texture2D to OpenCV Mat.
        /// Mat must be of MatType CV_8UC3 or CV_8UC4.
        /// Input texture's size and format will be changed to fit Mat.
        /// CV_8UC3 and CV_8UC4 will be converted to RGB24 and RGB32 textures respectively.
        /// </summary>
        /// <param name="mat">Mat to be converted into Texture2D.</param>
        /// <param name="tex">Reference to the output Texture2D.</param>
        public static void MatToTexture2D(Mat mat, ref Texture2D tex)
        {
            if (!mat.Empty())
            {
                TextureFormat textureFormat = new TextureFormat();
                int channels;

                if (mat.Type() == MatType.CV_8UC3)
                {
                    textureFormat = TextureFormat.RGB24;
                    channels = 3;
                }
                else if (mat.Type() == MatType.CV_8UC4)
                {
                    textureFormat = TextureFormat.RGBA32;
                    channels = 4;
                }
                else
                {
                    Debug.LogError("Unsupported MatType.");
                    return;
                }

                Cv2.Flip(mat, mat, FlipMode.X);
                if (tex.width != mat.Height || tex.height != mat.Width || tex.format != textureFormat)
                    tex.Resize(mat.Width, mat.Height, textureFormat, false);

                tex.LoadRawTextureData(mat.Data, mat.Rows * mat.Cols * channels * Marshal.SizeOf(typeof(byte)));
                tex.Apply();
            }
            else
            {
                Debug.LogError("Nothing to convert. Mat is empty.");
            }
        }


        /// <summary>
        /// Converts Unity Texture2D to OpenCV Mat.
        /// Texture2D must be of TextureFormat RGB24 (will result in 8UC3 Mat) or 
        /// RGB32 (8UC4 Mat). Mat will be resized to fit the texture.
        /// </summary>
        /// <param name="tex">Texture2D to be converted into Mat.</param>
        /// <param name="mat">Output OpenCV Mat.</param>
        public static void Texture2DToMat(Texture2D tex, ref Mat mat)
        {
            if (tex != null)
            {
                if (tex.width > 0 && tex.height > 0)
                {
                    MatType type = new MatType();
                    if (tex.format == TextureFormat.RGB24)
                    {
                        type = MatType.CV_8UC3;
                    }
                    else if (tex.format == TextureFormat.RGBA32)
                    {
                        type = MatType.CV_8UC4;
                    }
                    else
                    {
                        Debug.LogError("Unsupported TextureFormat.");
                        return;
                    }

                    if (tex.width != mat.Height || tex.height != mat.Width || mat.Type() != type)
                        mat = new Mat(tex.height, tex.width, type);

                    byte[] bytes;
                    Color32ArrayToByteArray(tex.GetPixels32(), out bytes);
                    mat.SetArray(0, 0, bytes);
                    Cv2.Flip(mat, mat, FlipMode.X);
                }
                else
                {
                    Debug.LogError("Invalid Texture2D size. Width and height must be >0.");
                }
            }
            else
            {
                Debug.LogError("Nothing to convert. Texture2D is null.");
            }
        }


        /// <summary>
        /// Converts Unity Color32 array to OpenCV Mat.
        /// Mat will be in corresponding type of CV_8UC4 with dimensions according to the params.
        /// Color32 array length must correspond to width * height.
        /// </summary>
        /// <param name="colors">Array of Color32 to be converted into OpenCV Mat.</param>
        /// <param name="mat">Output OpenCV Mat.</param>
        /// <param name="width">Width of output Mat.</param>
        /// <param name="height">Height of output Mat.</param>
        public static void Color32ArrayToMat(Color32[] colors, ref Mat mat, int width, int height)
        {
            if (colors.Length > 0)
            {
                if (mat.Width != width || mat.Height != height || mat.Type() != MatType.CV_8UC4)
                    mat = new Mat(height, width, MatType.CV_8UC4);

                byte[] bytes;
                Color32ArrayToByteArray(colors, out bytes);
                mat.SetArray(0, 0, bytes);
                Cv2.Flip(mat, mat, FlipMode.X);
            }
            else
            {
                Debug.LogError("Received empty color array.");
            }
        }


        /// <summary>
        /// Converts Color32 array to Texture2D.
        /// Length of Color32 array must correspond to texture width * height.
        /// </summary>
        /// <param name="colors">Input Color32 array.</param>
        /// <param name="tex">Reference to output Texture2D.</param>
		public static void Color32ArrayToTexture2D(Color32[] colors, ref Texture2D tex)
        {
            Color32ArrayToTexture2D(colors, ref tex, tex.width, tex.height);
        }


        /// <summary>
        /// Identical to Texture2D.SetPixels32(), but also sets the texture dimensions and format (always RGBA32). 
        /// </summary>
        /// <param name="colors">Input Color32 array.</param>
        /// <param name="tex">Output Texture2D.</param>
        /// <param name="width">Width of output Texture2D.</param>
        /// <param name="height">Height of ouput Texture2D.</param>
        public static void Color32ArrayToTexture2D(Color32[] colors, ref Texture2D tex, int width, int height)
        {
            if (tex.width != width || tex.height != height || tex.format != TextureFormat.RGBA32)
                tex.Resize(width, height, TextureFormat.RGBA32, false);

            if (colors.Length == width * height)
                tex.SetPixels32(colors);
        }


        /// <summary>
        /// Does the same as Texture2D.GetPixels32(). Exists for naming consistency.
        /// </summary>
        /// <param name="tex">Input Texture2D.</param>
        /// <param name="colors">Output Color32 array.</param>
        public static void Texture2DToColor32Array(Texture2D tex, out Color32[] colors)
        {
            colors = tex.GetPixels32();
        }


        /// <summary>
        /// Converts OpenCV Mat to Unity Color32 array.
        /// </summary>
        /// <param name="mat">Input OpenCV Mat.</param>
        /// <param name="colors">Output Color32 array.</param>
        public static void MatToColor32Array(Mat mat, out Color32[] colors)
        {
            // Slower than method below due to iteration.
            /*
			int size = mat.Width * mat.Height;
			colors = new Color32[size];
			System.IntPtr ptr = mat.Data;
			for (int i = 0; i < size; i++)
			{
				colors[i] = (Color32)Marshal.PtrToStructure(ptr, typeof(Color32));
				ptr += 4;
			}
			*/

            Texture2D tex = new Texture2D(1, 1);
            MatToTexture2D(mat, ref tex);
            colors = tex.GetPixels32();
        }


        /// <summary>
        /// Converts Color32 the array to byte array by using Marshal.Copy.
        /// </summary>
        /// <param name="colors">Input Color32 array.</param>
        /// <param name="bytes">Output byte array.</param>
        public static void Color32ArrayToByteArray(Color32[] colors, out byte[] bytes)
        {
            int length = Marshal.SizeOf(typeof(Color32)) * colors.Length;
            bytes = new byte[length];
            Marshal.Copy(CvUtil.GetIntPtr(colors), bytes, 0, length);
        }


        /// <summary>
        /// Converts Byte array to Unity Color32 array.
        /// </summary>
        /// <param name="bytes">Input Byte array.</param>
        /// <param name="colors">Output Color32 array.</param>
        public static void ByteArrayToColor32Array(byte[] bytes, out Color32[] colors)
        {
            int size = bytes.Length;
            colors = new Color32[size / 4];
            System.IntPtr ptr = CvUtil.GetIntPtr(bytes);
            for (int i = 0; i < size / 4; i++)
            {
                colors[i] = (Color32)Marshal.PtrToStructure(ptr, typeof(Color32));
                ptr = new System.IntPtr(ptr.ToInt32() + 4);
            }
        }

        /// <summary>
        /// Converts byte array to Color32 array.
        /// width * height must correspond to length of byte array times 4.
        /// </summary>
        /// <param name="bytes">Input byte array.</param>
        /// <param name="colors">Output Color32 array.</param>
        /// <param name="width">Width of the temporary texture to be used for conversion.</param>
        /// <param name="height">Height of the temporary texture to be used for conversion.</param>
        public static void ByteArrayToColor32ArrayFast(byte[] bytes, out Color32[] colors, int width, int height)
        {
            Texture2D tmp = new Texture2D(width, height, TextureFormat.RGBA32, false);
            ByteArrayToColor32ArrayFast(bytes, out colors, ref tmp);
        }


        /// <summary>
        /// Converts byte array to Color32 array.
        /// Input texture must be of dimension: width * height = bytes * 4.
        /// </summary>
        /// <param name="bytes">Input byte array.</param>
        /// <param name="colors">Output Color32 array.</param>
        /// <param name="tmp">Texture to be used for conversion between formats. It will retain image after conversion.</param>
        public static void ByteArrayToColor32ArrayFast(byte[] bytes, out Color32[] colors, ref Texture2D tmp)
        {
            ByteArrayToTexture2D(bytes, ref tmp, tmp.width, tmp.height);
            colors = tmp.GetPixels32();
        }


        /// <summary>
        /// Converts byte array to Texture2D.
        /// </summary>
        /// <param name="bytes">Input byte array.</param>
        /// <param name="tex">Output Texture2D.</param>
        public static void ByteArrayToTexture2D(byte[] bytes, ref Texture2D tex)
        {
            ByteArrayToTexture2D(bytes, ref tex, tex.width, tex.height);
        }


        /// <summary>
        /// Converts Byte array to Texture2D. 
        /// </summary>
        /// <param name="bytes">Bytes.</param>
        /// <param name="tex">Output Texture2D. Will be resized to given dimensions and of format RGBA32.</param>
        /// <param name="width">Width of the output texture.</param>
        /// <param name="height">Height of the output texture.</param>
        public static void ByteArrayToTexture2D(byte[] bytes, ref Texture2D tex, int width, int height)
        {
            if (tex.width != width || tex.height != height || tex.format != TextureFormat.RGBA32)
                tex.Resize(tex.width, tex.height, TextureFormat.RGBA32, false);

            tex.LoadRawTextureData(bytes);
        }


        /// <summary>
        /// Converts WebCamTexture to Texture2D.
        /// Texture2D's format and size will be changed to that of WebCamTexture.
        /// </summary>
        /// <param name="webTex">WebCamTexture to take image from.</param>
        /// <param name="tex">Output Texture2D.</param>
        public static void WebCamTextureToTexture2D(WebCamTexture webTex, ref Texture2D tex)
        {
            if (webTex.width != tex.width || webTex.height != tex.height || tex.format != TextureFormat.RGBA32)
                tex.Resize(webTex.width, webTex.height, TextureFormat.RGBA32, false);

            tex.SetPixels32(webTex.GetPixels32());
            tex.Apply();
        }


        /// <summary>
        /// Converts Unity WebCamTexture to OpenCV Mat.
        /// </summary>
        /// <param name="webTex">WebCamTexture to take image from.</param>
        /// <param name="mat">Output OpenCV Mat. It will have dimensions of WebCamTexture and 8UC4 format.</param>
        public static void WebCamTextureToMat(WebCamTexture webTex, ref Mat mat)
        {
            Color32ArrayToMat(webTex.GetPixels32(), ref mat, webTex.width, webTex.height);
        }


        /// <summary>
        /// Converts RenderTexture to Texture2D.
        /// </summary>
        /// <param name="rendTex">Input RenderTexture.</param>
        /// <param name="tex">Output Texture2D. Will have dimensions of RenderTexture and RGBA32 format.</param>
        public static void RenderTextureToTexture2D(RenderTexture rendTex, ref Texture2D tex)
        {
            UnityEngine.Rect _rect = new UnityEngine.Rect();

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = rendTex;

            _rect.size = new Vector2(rendTex.width, rendTex.height);

            if (tex.width != rendTex.width || tex.height != rendTex.height || tex.format != TextureFormat.RGBA32)
                tex.Resize(rendTex.width, rendTex.height, TextureFormat.RGBA32, false);

            tex.ReadPixels(_rect, 0, 0);
            tex.Apply();
            RenderTexture.active = currentRT;
        }


        /// <summary>
        /// Converts Texture2D to RenderTexture. 
        /// </summary>
        /// <param name="tex">Input Texture2D.</param>
        /// <param name="rendTex">Output RenderTexture. It will have dimensions of Texture2D and ARGB32 format.</param>
        public static void Texture2DToRenderTexture(Texture2D tex, ref RenderTexture rendTex)
        {
            if (rendTex.width != tex.width || rendTex.height != tex.height)
                rendTex = new RenderTexture(tex.width, tex.height, 24, RenderTextureFormat.ARGB32,
                                            RenderTextureReadWrite.Default);
            Graphics.Blit(tex, rendTex);
        }


        /// <summary>
        /// Converts WebCamTexture to RenderTexture. Equivalent to Graphics.Blit, exists for naming consistency.
        /// </summary>
        /// <param name="webTex">Input WebCamTexture.</param>
        /// <param name="rendTex">Output RenderTexture.</param>
        public static void WebCamTextureToRenderTexture(WebCamTexture webTex, ref RenderTexture rendTex)
        {
            Graphics.Blit(webTex, rendTex);
        }

    }
}
