using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using static Xamarin_Android.Droid.MatrixMath;
using Org.Webrtc;
using System.Net.Http;
using Java.Nio;
using Android.Runtime;
using Newtonsoft.Json.Linq;

namespace Xamarin_Android.Droid
{
    class VideoRendererWithControls : SurfaceViewRenderer//, View.IOnTouchListener
    {
        float navHeading = 0;
        float navPitch = 0;
        float[] navLocation = new float[] { 0, 0, 0 };
        bool isFirstDown = false;
        float fingerDownX;
        float fingerDownY;
        float downPitch = 0;
        float downHeading = 0;
        float[] downLocation = new float[] { 0, 0, 0 };
        float[,] navTransform;
        //private OnMotionEventListener mListener;
        IOnMotionEventListener mListener;
        float dx;
        float dy;
        float scaleFactor;
        float zoomFingerDownX;
        float zoomFingerDownY;

        Mode mode = Mode.NONE;

        enum Mode
        {
            DRAG,
            ZOOM,
            NONE
        };



        public VideoRendererWithControls(Context context) : base(context)
        {
        }

        public VideoRendererWithControls(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public void SetEventListener(IOnMotionEventListener eventListener)
        {
            mListener = eventListener;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (mListener == null)
            {
                return false;
            }

            int pointerCount = e.PointerCount;
            MotionEvent.PointerCoords pointerCoords = new MotionEvent.PointerCoords();

            switch (e.Action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                    mode = Mode.DRAG;
                    dx = 0;
                    dy = 0;

                    fingerDownX = e.RawX;
                    fingerDownY = e.RawY;

                    downPitch = navPitch;
                    downHeading = navHeading;
                    downLocation[0] = navLocation[0];
                    downLocation[1] = navLocation[1];
                    downLocation[2] = navLocation[2];

                    break;
                case MotionEventActions.Move:
                    if (mode == Mode.DRAG)
                    {
                        dx = e.RawX - fingerDownX;
                        dy = e.RawY - fingerDownY;

                    }
                    else if (mode == Mode.ZOOM)
                    {
                        e.GetPointerCoords(1, pointerCoords);
                        scaleFactor = pointerCoords.Y - zoomFingerDownY;
                    }
                    break;
                case MotionEventActions.PointerDown:
                    mode = Mode.ZOOM;
                    e.GetPointerCoords(1, pointerCoords);
                    zoomFingerDownY = pointerCoords.Y;
                    isFirstDown = true;
                    break;

                case MotionEventActions.Up:
                    mode = Mode.NONE;
                    break;
                case MotionEventActions.PointerUp:
                    mode = Mode.NONE;
                    break;
            }

            if (mode == Mode.DRAG && (Math.Abs(dx) + Math.Abs(dy) > 20))
            {
                var dpitch = (float)0.005 * dy;
                var dheading = (float)0.005 * dx;

                navHeading = downHeading + dheading;
                navPitch = downPitch - dpitch;
                var localTransform = MatMultiply(MatRotateY(navHeading), MatRotateZ(navPitch));
                navTransform = MatMultiply(MatTranslate(navLocation), localTransform);

                ToBuffer();
            }
            else if (mode == Mode.ZOOM && Math.Abs(scaleFactor) > 10)
            {
                //MotionEvent.PointerCoords pointerCoords = new MotionEvent.PointerCoords();
                //e.GetPointerCoords(1, pointerCoords);

                //float dy = pointerCoords.Y - fingerDownY;

                if (isFirstDown)
                {
                    isFirstDown = false;
                    return true;
                }

                float dist = -0.3f * scaleFactor;//-dy;

                navLocation[0] = downLocation[0] + dist * navTransform[0, 0];
                navLocation[1] = downLocation[1] + dist * navTransform[0, 1];
                navLocation[2] = downLocation[2] + dist * navTransform[0, 2];

                navTransform[3, 0] = navLocation[0];
                navTransform[3, 1] = navLocation[1];
                navTransform[3, 2] = navLocation[2];

                ToBuffer();
            }             
            return true;
        } 

        private void ToBuffer()
        {
            if(mListener == null)
            {
                return;
            }
            float[] eye = { navTransform[3, 0], navTransform[3, 1], navTransform[3, 2] };
            float[] lookat = { navTransform[3, 0] + navTransform[0, 0], navTransform[3, 1] + navTransform[0, 1], navTransform[3, 2] + navTransform[0, 2] };
            float[] up = { navTransform[1, 0], navTransform[1, 1], navTransform[1, 2] };

            string data = eye[0] + ", " + eye[1] + ", " + eye[2] + ", " +
                        lookat[0] + ", " + lookat[1] + ", " + lookat[2] + ", " +
                        up[0] + ", " + up[1] + ", " + up[2];

            var content = new JObject();
            content.Add("type", "camera-transform-lookat");
            content.Add("body", data);
            //var content = new StringContent(data, Encoding.UTF8, "camera-transform-lookat");
            Console.WriteLine(content.ToString(Newtonsoft.Json.Formatting.None));
            ByteBuffer byteBuffer = ByteBuffer.Wrap(Encoding.ASCII.GetBytes(content.ToString(Newtonsoft.Json.Formatting.None)));
            DataChannel.Buffer buffer = new DataChannel.Buffer(byteBuffer, false);
            mListener.SendTransofrm(buffer);

        }

        public interface IOnMotionEventListener
        {
            void SendTransofrm(DataChannel.Buffer server);
        }
    }
}