using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Toguchi.Facial
{
    public class VRMIFacialRemapper : MonoBehaviour
    {
        public Animator animator;
        public FacialSource facialSource;
        public AxisConvert headAxis;
        public EyeMapping eyeMapping;
        public BlendShapeMapping blendShapeMapping;
        
        private Transform _head;
        private Transform _eyeR;
        private Transform _eyeL;
        private SkinnedMeshRenderer _skinnedMesh;

        [Button]
        private void Initialize()
        {
            blendShapeMapping.initialize();
            foreach (var remapper in blendShapeMapping.reMappers)
            {
                remapper.source = facialSource.blendShapes;
                remapper.target = blendShapeMapping.skinnedMesh;
                
                remapper.Initialize();
            }
        }
        
        
        void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            _head = animator.GetBoneTransform(HumanBodyBones.Head);

            if (eyeMapping.eyeInterface == EyeMapping.EyeInterface.Bone)
            {
                _eyeR = animator.GetBoneTransform(HumanBodyBones.RightEye);
                _eyeL = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            }

            _skinnedMesh = blendShapeMapping.skinnedMesh;
        }

        void LateUpdate()
        {
            // Head
            var headSource = facialSource.head;
            _head.localRotation = Quaternion.Euler(headAxis.GetAngle(headSource.localRotation.eulerAngles));
            
            // Eye
            if (eyeMapping.eyeInterface == EyeMapping.EyeInterface.Bone)
            {
                var right = eyeMapping.ConvertAngle(facialSource.eyeR.localRotation.eulerAngles);
                var left = eyeMapping.ConvertAngle(facialSource.eyeL.localRotation.eulerAngles);
                if (eyeMapping.eyeInvert)
                {
                    var tmp = right;
                    right = left;
                    left = tmp;
                }
                
                Debug.Log("RightSource : " + facialSource.eyeR.localRotation.eulerAngles);
                Debug.Log("Right : " + right);

                _eyeR.localRotation = Quaternion.Euler(right);
                _eyeL.localRotation = Quaternion.Euler(left);
            }
            
            /*
             * Eye BlendShapeの場合の実装
            */
            
            // BlendShape
            var blinkL = Mathf.Clamp(facialSource.blendShapes.GetBlendShapeWeight(1) * blendShapeMapping.multiplier, 0f, 100f);
            var blinkR = Mathf.Clamp(facialSource.blendShapes.GetBlendShapeWeight(0) * blendShapeMapping.multiplier, 0f, 100f);
            var indexL = _skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeMapping.blinkL);
            var indexR = _skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeMapping.blinkR);
            if (blendShapeMapping.invertBlink)
            {
                _skinnedMesh.SetBlendShapeWeight(indexR, blinkL);
                _skinnedMesh.SetBlendShapeWeight(indexL, blinkR);
            }
            else
            {
                _skinnedMesh.SetBlendShapeWeight(indexR, blinkR);
                _skinnedMesh.SetBlendShapeWeight(indexL, blinkL);
            }
            blendShapeMapping.reMappers.ForEach(mapper => mapper.UpdateShapes());
            
        }
    }
    
    

    [System.Serializable]
    public class FacialSource
    {
        public SkinnedMeshRenderer blendShapes;
        public Transform head;
        public Transform eyeR;
        public Transform eyeL;
    }
    
    

    [System.Serializable]
    public class EyeMapping
    {
        public enum EyeInterface
        {
            BlendShape, Bone
        }
        public bool eyeInvert;
        public EyeInterface eyeInterface;
        public AxisConvert eyeAxis;

        public Vector3 angleMultiplier;

        public Vector3 ConvertAngle(Vector3 sourceRotation)
        {
            var angle = eyeAxis.GetAngle(sourceRotation);
            angle.x *= angleMultiplier.x;
            angle.y *= angleMultiplier.y;
            angle.z *= angleMultiplier.z;

            return angle;
        }
    }
    
    

    [System.Serializable]
    public class BlendShapeMapping
    {
        public SkinnedMeshRenderer skinnedMesh;
        public bool invertBlink;
        [ValueDropdown("targetShapes")] public string blinkR;
        [ValueDropdown("targetShapes")] public string blinkL;
        
        public float multiplier;
        
        
        public List<BlendShapeReMapper> reMappers = new List<BlendShapeReMapper>();
            
        public string[] targetShapes = new string[0];
        
        public void initialize()
        {
            targetShapes = new string[skinnedMesh.sharedMesh.blendShapeCount];
            for (int i = 0; i < targetShapes.Length; i++)
            {
                targetShapes[i] = skinnedMesh.sharedMesh.GetBlendShapeName(i);
            }
        }

        
        [System.Serializable]
        public class BlendShapeReMapper
        {
            public SkinnedMeshRenderer source;
            public SkinnedMeshRenderer target;

            public float gain = 1f;
            public AnimationCurve sharpenCurve;
            
            public bool useMasterShape = true;
            [ValueDropdown("sourceShapes")] 
            public string masterShape;

            public AnimationCurve masterCurve;
            
            
            public Vector2 clamp = new Vector2(0f, 1f);
            
            [ValueDropdown("sourceShapes")]
            public string[] sourceShape;

            public List<TargetShape> targetShape = new List<TargetShape>();

            [ValueDropdown("targetShapes")] 
            public string setTargetShape;

            public string[] sourceShapes = new string[0];
            public string[] targetShapes = new string[0];

            private float[] distances = new float[0];
            
            [Button]
            private void Set()
            {
                var shape = new TargetShape();
                shape.name = setTargetShape;
                shape.parameters = new float[sourceShape.Length];

                for (int i = 0; i < shape.parameters.Length; i++)
                {
                    shape.parameters[i] =
                        source.GetBlendShapeWeight(source.sharedMesh.GetBlendShapeIndex(sourceShape[i]));
                }
                
                targetShape.Add(shape);
            }
            
            public void Initialize()
            {
                sourceShapes = new string[source.sharedMesh.blendShapeCount];
                for (int i = 0; i < sourceShapes.Length; i++)
                {
                    sourceShapes[i] = source.sharedMesh.GetBlendShapeName(i);
                }

                targetShapes = new string[target.sharedMesh.blendShapeCount];
                for (int i = 0; i < targetShapes.Length; i++)
                {
                    targetShapes[i] = target.sharedMesh.GetBlendShapeName(i);
                }
            }

            public void UpdateShapes()
            {
                UpdateDistances();
                
                Utility.Normalize(ref distances);
                Utility.OneMinus(ref distances);
                Utility.ApplyCurve(ref distances, sharpenCurve);
                Utility.Gain(ref distances, gain);
                Utility.SizeClamp(ref distances, clamp.x, clamp.y);
                if (useMasterShape)
                {
                    var master =
                        masterCurve.Evaluate(
                            source.GetBlendShapeWeight(source.sharedMesh.GetBlendShapeIndex(masterShape)) / 100f);
                    Utility.Gain(ref distances, master);
                }
                Utility.Gain(ref distances, 100f);

                for (int i = 0; i < targetShape.Count; i++)
                {
                    target.SetBlendShapeWeight(target.sharedMesh.GetBlendShapeIndex(targetShape[i].name), distances[i]);
                }
            }

            private void UpdateDistances()
            {
                if (distances.Length != targetShape.Count)
                {
                    distances = new float[targetShape.Count];
                }

                if (distances.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < distances.Length; i++)
                {
                    distances[i] = GetDistance(targetShape[i].parameters, sourceShape);
                }
            }

            private float GetDistance(float[] destination, string[] sources)
            {
                var distance = 0f;
                for (int i = 0; i < sources.Length; i++)
                {
                    distance += Mathf.Abs(destination[i] -
                                          source.GetBlendShapeWeight(source.sharedMesh.GetBlendShapeIndex(sources[i])));
                }

                return distance;
            }
        }
        

        [System.Serializable]
        public class TargetShape
        {
            public string name;
            public float[] parameters;
        }
    }
    
    

    [System.Serializable]
    public class AxisConvert
    {
        public Vector3 x;
        public Vector3 y;
        public Vector3 z;
        
        private Vector3 _angle = Vector3.zero;
        public Vector3 GetAngle(Vector3 rotation)
        {
            rotation.x = ClampAngle(rotation.x);
            rotation.y = ClampAngle(rotation.y);
            rotation.z = ClampAngle(rotation.z);
            
            _angle.x = rotation.x * x.x + rotation.y * x.y + rotation.z * x.z;
            _angle.y = rotation.x * y.x + rotation.y * y.y + rotation.z * y.z;
            _angle.z = rotation.x * z.x + rotation.y * z.y + rotation.z * z.z;

            return _angle;
        }

        float ClampAngle(float angle)
        {
            if (angle > 180f)
            {
                angle = -(360f - angle);
            }

            return angle;
        }
    }

    
    
    public class Utility
    {
        public static float Sum(float[] nums)
        {
            var sum = 0f;
            for (int i = 0; i < nums.Length; i++)
            {
                sum += nums[i];
            }

            return sum;
        }

        public static float Max(float[] nums)
        {
            if (nums.Length <= 0)
            {
                return 0f;
            }

            var max = nums[0];

            for (int i = 1; i < nums.Length; i++)
            {
                if (nums[i] > max)
                {
                    max = nums[i];
                }
            }

            return max;
        }


        public static float Average(float[] nums)
        {
            var avg = Sum(nums) / nums.Length;
            return avg;
        }

        public static void OneMinus(ref float[] nums, bool normalize = true)
        {
            if (normalize)
            {
                Normalize(ref nums);
            }

            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] = 1f - nums[i];
            }
        }

        public static void Sharpen(ref float[] nums, float multiplier)
        {
            var avg = Average(nums);

            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] += (nums[i] - avg) * multiplier;
            }
        }

        public static void ApplyCurve(ref float[] nums, AnimationCurve curve)
        {
            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] = curve.Evaluate(nums[i]);
            }
        }

        public static void Normalize(ref float[] nums, float maxNum = 1f)
        {
            var max = Max(nums);

            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] = nums[i] / max * maxNum;
            }
        }

        public static void Gain(ref float[] nums, float gain)
        {
            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] *= gain;
            }
        }

        public static void SizeClamp(ref float[] nums, float min, float max)
        {
            var sum = Sum(nums);
        
            for (int i = 0; i < nums.Length; i++)
            {
                nums[i] = nums[i] / sum * (max - min) + min;
            }
        }
    }
}
