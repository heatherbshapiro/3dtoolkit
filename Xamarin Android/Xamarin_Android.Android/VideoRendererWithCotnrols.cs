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

namespace Xamarin_Android.Droid
{
    class VideoRendererWithCotnrols : SurfaceViewRenderer, View.IOnTouchListener
    {
        float navHeading = 0;
        float navPitch = 0;
        float[] navLocation = new float[] { 0, 0, 0 };
        bool isFingerDown = false;
        float fingerDownX;
        float fingerDownY;
        float downPitch = 0;
        float downHeading = 0;
        float[] downLocation = new float[] { 0, 0, 0 };
        float[,] navTransform;
        //private OnMotionEventListener mListener;
        IOnMotionEventListener mListener;
        


        public VideoRendererWithCotnrols(Context context) : base(context)
        {
        }

        public VideoRendererWithCotnrols(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public void SetEventListener(IOnMotionEventListener eventListener)
        {
            mListener = eventListener;
        }

        public bool OnTouch(View v, MotionEvent e)
        {
         
            
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        isFingerDown = true;
                        fingerDownX = e.RawX;
                        fingerDownY = e.RawY;

                        downPitch = navPitch;
                        downHeading = navHeading;
                        downLocation[0] = navLocation[0];
                        downLocation[1] = navLocation[1];
                        downLocation[2] = navLocation[2];

                        break;
                    case MotionEventActions.Move:
                        if (isFingerDown)
                        {
                            if (e.PointerCount == 1)
                                {
                                var dx = e.RawX - fingerDownX;
                                var dy = e.RawY - fingerDownY;

                                var dpitch = (float)0.005 * dy;
                                var dheading = (float)0.005 * dx;

                                navHeading = downHeading - dheading;
                                navPitch = downPitch + dpitch;
                                var localTransform = MatMultiply(MatRotateY(navHeading), MatRotateZ(navPitch));
                                navTransform = MatMultiply(MatTranslate(navLocation), localTransform);

                                ToBuffer();
                            } else if (e.PointerCount == 2){
                                MotionEvent.PointerCoords pointerCoords = new MotionEvent.PointerCoords();
                                e.GetPointerCoords(1, pointerCoords);

                                float dy = pointerCoords.Y - fingerDownY;

                                float dist = -dy;

                                navLocation[0] = downLocation[0] + dist * navTransform[0, 0];
                                navLocation[1] = downLocation[1] + dist * navTransform[0, 1];
                                navLocation[2] = downLocation[2] + dist * navTransform[0, 2];

                                navTransform[3, 0] = navLocation[0];
                                navTransform[3, 1] = navLocation[1];
                                navTransform[3, 2] = navLocation[2];

                                ToBuffer();
                            }
                        }
                        break;
                    case MotionEventActions.Up:
                        isFingerDown = false;
                        break;
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

            var content = new StringContent(data, Encoding.UTF8, "camera-transform-lookat");
            ByteBuffer byteBuffer = ByteBuffer.Wrap(Encoding.ASCII.GetBytes(content.ToString()));
            DataChannel.Buffer buffer = new DataChannel.Buffer(byteBuffer, false);
            mListener.SendTransofrm(buffer);

        }

        public interface IOnMotionEventListener
        {
            void SendTransofrm(DataChannel.Buffer server);
        }
    }
}