using UnityEngine;
using System.Collections.Generic;

namespace None {
	
    public static class VectorTools
    {
        public static float GetDirectionDot(this Vector2 originalVector, Vector2 directionVector)
        {
            Vector2 result1 = new Vector2(-1 * originalVector.y, originalVector.x);
            return Vector2.Dot(result1, directionVector);
        }
        
        public static Vector2 Perpendicular(this Vector2 originalVector, Vector2 directionVector, bool forceSwap = false)
        {
            Vector2 result1 = new Vector2(-1 * originalVector.y, originalVector.x);
            float dotResult = Vector2.Dot(result1, directionVector);

            return originalVector.Perpendicular(dotResult);
        }
        
        public static Vector2 Perpendicular(this Vector2 originalVector, float directionDot, bool forceSwap = false)
        {
            Vector2 result1 = new Vector2(-1 * originalVector.y, originalVector.x);
            Vector2 result2 = new Vector2(originalVector.y, -1 * originalVector.x);

            if (directionDot >= -0.1)
                return forceSwap ? result2 : result1;
            else
                return forceSwap ? result1 : result2;
        }

        public static Vector2 RightPerp(this Vector2 vector)
        {
            return new Vector2(vector.y, -1 * vector.x);
        }

        public static Vector2 LeftPerp(this Vector2 vector)
        {
            return new Vector2(-1 * vector.y, vector.x);
        }

        public static float ConvertToAngle(this Vector2 vector)
        {
            if (vector == Vector2.right)
                return 0;
            
            var angle = Mathf.Acos(vector.x / vector.magnitude);

            if (vector.y <= Mathf.Epsilon)
                angle = (2 * Mathf.PI) - angle;

            if (angle < Mathf.Epsilon)
                angle = (2 * Mathf.PI) + angle;

            return angle;
        }

        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }

        public static Vector3 ToVector3(this Vector2 vector)
        {
            return new Vector3(vector.x, 0, vector.y);
        }
    }

	public class SteeringCircleHandler : MonoBehaviour {
		/// <summary>Mask for the raycast placement</summary>
		public LayerMask mask;

		public Transform targetTransform;
        public Transform startTransform;
        private bool hasSetDirection;


        public float turnRadius = 3;
        public float angleStep = 0.5f;

        public bool ignoreTooSmall;

        public List<Vector2> finalPath = new List<Vector2>();

        Camera cam;


        private Vector2 startPos;
        private Vector2 endPos;

        private Vector2 startDir;
        private Vector2 endDir;

        private Vector2 startCircleCenterPos;
        private Vector2 endCircleCenterPos;

        private Vector2 startCircleExitPos;
        private Vector2 endCircleEnterPos;

        private float startCircleExitAngle;
        private float endCircleEnterAngle;

        public bool swapStartPerpendicular;
        public bool swapEndPerpendicular;

        public bool forcePerpendicularSwapStart;
        public bool forcePerpendicularSwapEnd;

        public void Start () {
			//Cache the Main Camera
			cam = Camera.main;
		}

        private void Update()
        {
            CalculteFormationPath();
        }

        public void OnGUI () {
			if (cam != null) {
                if (Event.current.type == EventType.MouseDown)
                { 
                    UpdateTargetPosition();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    UpdateTargetDirection();
                    hasSetDirection = true;
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    UpdateTargetDirection();
                    hasSetDirection = false;
                }
			}
		}

        public void UpdateTargetPosition()
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, mask))
            {
                Vector3 newPosition = hit.point;

                if (newPosition != targetTransform.position)
                {
                    startTransform.position = targetTransform.position;
                    targetTransform.position = newPosition;
                }
            }
        }

        public void UpdateTargetDirection()
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, mask))
            {
                Vector3 newDirection = hit.point;

                Vector3 lookDir = newDirection - targetTransform.position;
                lookDir.y = 0;

                if (!hasSetDirection)
                    startTransform.forward = targetTransform.forward;

                targetTransform.rotation = Quaternion.LookRotation(lookDir);

                CalculteFormationPath();
            }
        }

        public void CalculteFormationPath()
        {
            startPos = startTransform.position.ToVector2();
            startDir = startTransform.forward.ToVector2().normalized * -1f;

            endPos = targetTransform.position.ToVector2();
            endDir = targetTransform.forward.ToVector2().normalized * -1f;


            Vector2 startPerpendicular, endPerpendicular;
            Vector2 directionVec;

            Vector2 centerDir;

            // 1) Calculate the starting steering circle
            directionVec = (endPos - startPos).normalized;
            startPerpendicular = startDir.Perpendicular(directionVec, forcePerpendicularSwapStart);
            startCircleCenterPos = startPos + startPerpendicular * turnRadius;

            // 2) Calculate the ending steering circle
            endPerpendicular = endDir.Perpendicular(directionVec * -1f, forcePerpendicularSwapEnd);
            endCircleCenterPos = endPos + endPerpendicular * turnRadius;

            //swapPerpendicular = !swapPerpendicular;

            centerDir = endCircleCenterPos - startCircleCenterPos;

            bool tooSmall = false;

            if (centerDir.magnitude < 2 * turnRadius && !ignoreTooSmall)
            {
                startDir *= -1;
                //endDir *= -1;

                startPerpendicular = startDir.Perpendicular(directionVec, forcePerpendicularSwapStart);
                startCircleCenterPos = startPos + startPerpendicular * turnRadius;

                tooSmall = true;

                //endPerpendicular = endDir.Perpendicular(directionVec * -1f, forcePerpendicularSwapEnd);
                //endCircleCenterPos = endPos + endPerpendicular * turnRadius;
            }



            int sideStart;
            int sideEnd;

            if (startPerpendicular == startDir.RightPerp())
                sideStart = 0;
            else
                sideStart = 1;

            if (endPerpendicular == endDir.RightPerp())
                sideEnd = 0;
            else
                sideEnd = 1;

            // 3) Calculate the starting circle exit point    
            if (sideStart != sideEnd)
            {
                float halfCenterDistance = centerDir.magnitude / 2;
                float angle1 = turnRadius > halfCenterDistance ? 1 : Mathf.Acos(turnRadius / halfCenterDistance);
                float angle2 = centerDir.ConvertToAngle();

                if (sideStart == 1 && sideEnd == 0)
                    startCircleExitAngle = angle2 + angle1;
                else
                    startCircleExitAngle = angle2 - angle1;

                startCircleExitPos.x = startCircleCenterPos.x + turnRadius * Mathf.Cos(startCircleExitAngle);
                startCircleExitPos.y = startCircleCenterPos.y + turnRadius * Mathf.Sin(startCircleExitAngle);
            }
            else
            {
                if (sideStart == 1)
                    startCircleExitPos = centerDir.LeftPerp().normalized * turnRadius;
                else
                    startCircleExitPos = centerDir.RightPerp().normalized * turnRadius;

                startCircleExitAngle = startCircleExitPos.ConvertToAngle();
                startCircleExitPos = startCircleCenterPos + startCircleExitPos;
            }

            // 4) Calculate the ending circle entry point
            if (sideStart != sideEnd)
            {
                float halfCenterDistance = centerDir.magnitude / 2;
                float angle1 = turnRadius > halfCenterDistance ? 1 : Mathf.Acos(turnRadius / halfCenterDistance);
                float angle2 = (startCircleCenterPos - endCircleCenterPos).ConvertToAngle();

                if (sideStart == 1 && sideEnd == 0)
                    endCircleEnterAngle = angle2 + angle1;
                else
                    endCircleEnterAngle = angle2 - angle1;

                endCircleEnterPos.x = endCircleCenterPos.x + turnRadius * Mathf.Cos(endCircleEnterAngle);
                endCircleEnterPos.y = endCircleCenterPos.y + turnRadius * Mathf.Sin(endCircleEnterAngle);
            }
            else
            {
                if (sideEnd == 1)
                    endCircleEnterPos = centerDir.LeftPerp().normalized * turnRadius;
                else
                    endCircleEnterPos = centerDir.RightPerp().normalized * turnRadius;

                endCircleEnterAngle = endCircleEnterPos.ConvertToAngle();
                endCircleEnterPos = endCircleCenterPos + endCircleEnterPos;
            }

            // 5) Check for small angle deformations
            if(!forcePerpendicularSwapStart)
            {
                Vector2 startVec = startPos - startCircleCenterPos;
                Vector2 endVec = startCircleExitPos - startCircleCenterPos;

                float startAngle = startVec.ConvertToAngle();
                float endAngle = startCircleExitAngle; //endVec.ConvertToAngle();

                if (Mathf.Abs(startAngle - endAngle) < angleStep)
                {
                    forcePerpendicularSwapStart = true;
                    CalculteFormationPath();
                    return;
                }
            }
            if (!forcePerpendicularSwapEnd)
            {
                Vector2 startVec = endCircleEnterPos - endCircleCenterPos;
                Vector2 endVec = endPos - endCircleCenterPos;

                float startAngle = endCircleEnterAngle; //startVec.ConvertToAngle();
                float endAngle = endVec.ConvertToAngle();

                if (Mathf.Abs(startAngle - endAngle) < angleStep)
                {
                    forcePerpendicularSwapEnd = true;
                    CalculteFormationPath();
                    return;
                }
            }

            GeneratePathArray(sideStart, sideEnd);

            forcePerpendicularSwapStart = false;
            forcePerpendicularSwapEnd = false;
        }

        public void GeneratePathArray(int directionStart, int directionEnd)
        {
            var path = new List<Vector2>();

            ///////////////////////////////////////////////////////////////////////     
            // Generate points on the starting circle
            Vector2 startVec = startPos - startCircleCenterPos;
            Vector2 endVec = startCircleExitPos - startCircleCenterPos;

            float startAngle = startVec.ConvertToAngle();
            float endAngle = startCircleExitAngle; //endVec.ConvertToAngle();

            // Generate points on the starting circle
            if (directionStart == 0) // clockwise
                GeneratePath_Clockwise(startAngle, endAngle, startCircleCenterPos, ref path);
            else // Counter-clockwise
                GeneratePath_CounterClockwise(startAngle, endAngle, startCircleCenterPos, ref path);

            ///////////////////////////////////////////////////////////////////////
            // Generate points on the ending circle
            startVec = endCircleEnterPos - endCircleCenterPos;
            endVec = endPos - endCircleCenterPos;

            startAngle = endCircleEnterAngle; //startVec.ConvertToAngle();
            endAngle = endVec.ConvertToAngle();

            // Generate points on the starting circle
            if (directionEnd == 0) // clockwise
                GeneratePath_Clockwise(startAngle, endAngle, endCircleCenterPos, ref path);
            else // Counter-clockwise
                GeneratePath_CounterClockwise(startAngle, endAngle, endCircleCenterPos, ref path);

            // Give the path to the formation
            finalPath = path;
        }

        public void GeneratePath_Clockwise (float startAngle, float endAngle, Vector3 center, ref List<Vector2> path)
        {
            if (Mathf.Abs(startAngle - endAngle) > angleStep && startAngle > endAngle)
                endAngle += 2 * Mathf.PI;

            float curAngle = startAngle;
            while (curAngle < endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(curAngle),
                    y = center.y + turnRadius * Mathf.Sin(curAngle)
                };
                path.Add(p);

                curAngle += angleStep;
            }

            if (curAngle != endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(endAngle),
                    y = center.y + turnRadius * Mathf.Sin(endAngle)
                };
                path.Add(p);
            }
        }

        public void GeneratePath_CounterClockwise (float startAngle, float endAngle, Vector3 center, ref List<Vector2> path)
        {
            if (Mathf.Abs(startAngle - endAngle) > angleStep && startAngle < endAngle)
                startAngle += 2 * Mathf.PI;

            float curAngle = startAngle;
            while (curAngle > endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(curAngle),
                    y = center.y + turnRadius * Mathf.Sin(curAngle)
                };
                path.Add(p);

                curAngle -= angleStep;
            }

            if (curAngle != endAngle)
            {
                var p = new Vector2
                {
                    x = center.x + turnRadius * Mathf.Cos(endAngle),
                    y = center.y + turnRadius * Mathf.Sin(endAngle)
                };
                path.Add(p);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startTransform.position, startTransform.position + startTransform.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.forward);
            Gizmos.color = Color.white;



            if (finalPath != null)
            {
                Vector3 previousPoint = Vector3.zero;

                for (int i = 0; i < finalPath.Count; i++)
                {
                    Vector3 currentPoint = finalPath[i].ToVector3();

                    Gizmos.DrawSphere(currentPoint, 0.25f);

                    if (i != 0)
                    {
                        Gizmos.DrawLine(previousPoint, currentPoint);
                    }

                    previousPoint = currentPoint;
                }
            }
        }
    }
}
