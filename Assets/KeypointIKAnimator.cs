using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class KeypointIKAnimator : MonoBehaviour
{
    // Essential Mixamo bones
    public Transform hips;          // Root hip/pelvis bone
    public Transform spine;         // Spine bone
    public Transform leftUpperArm;  // Left shoulder/upper arm
    public Transform rightUpperArm; // Right shoulder/upper arm
    public Transform leftLowerArm;  // Left elbow/lower arm
    public Transform rightLowerArm; // Right elbow/lower arm
    public Transform leftHand;      // Left wrist/hand
    public Transform rightHand;     // Right wrist/hand
    public Transform leftUpperLeg;  // Left hip/upper leg
    public Transform rightUpperLeg; // Right hip/upper leg
    public Transform leftLowerLeg;  // Left knee/lower leg
    public Transform rightLowerLeg; // Right knee/lower leg
    public Transform leftFoot;      // Left ankle/foot
    public Transform rightFoot;     // Right ankle/foot
    public Transform head;          // Head bone

    // Animation settings
    public float frameRate = 0.1f;
    public float depthScale = 0.01f;
    public float heightScale = 0.01f;
    public float zOffset = 1.0f;
    public float yOffset = 0f;
    public float imageHeight = 720f;

    private List<Vector3[]> keypointsFrames = new List<Vector3[]>();
    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        LoadCSV();
    }

    void Update()
    {
        if (keypointsFrames.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % keypointsFrames.Count;
            ApplyPose();
        }
    }

    void ApplyPose()
    {
        Vector3[] frame = keypointsFrames[currentFrame];

        // Apply positions to essential bones
        hips.position = frame[0];          // Hips position
        leftUpperArm.position = frame[1];   // Left shoulder
        rightUpperArm.position = frame[2];  // Right shoulder
        leftLowerArm.position = frame[3];   // Left elbow
        rightLowerArm.position = frame[4];  // Right elbow
        leftHand.position = frame[5];       // Left wrist
        rightHand.position = frame[6];      // Right wrist
        leftUpperLeg.position = frame[7];   // Left hip
        rightUpperLeg.position = frame[8];  // Right hip
        leftLowerLeg.position = frame[9];   // Left knee
        rightLowerLeg.position = frame[10]; // Right knee
        leftFoot.position = frame[11];      // Left ankle
        rightFoot.position = frame[12];     // Right ankle
        head.position = frame[13];          // Head position

        // Update rotations based on positions
        UpdateLimbRotations();
        UpdateSpineRotation();
    }

    void UpdateLimbRotations()
    {
        // Update arm rotations
        UpdateLimbRotation(leftUpperArm, leftLowerArm, leftHand);
        UpdateLimbRotation(rightUpperArm, rightLowerArm, rightHand);

        // Update leg rotations
        UpdateLimbRotation(leftUpperLeg, leftLowerLeg, leftFoot);
        UpdateLimbRotation(rightUpperLeg, rightLowerLeg, rightFoot);
    }

    void UpdateSpineRotation()
    {
        Vector3 spineDirection = (head.position - hips.position).normalized;
        if (spineDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(spineDirection);
            spine.rotation = Quaternion.Slerp(spine.rotation, targetRotation, 0.5f);
        }
    }

    void UpdateLimbRotation(Transform upperBone, Transform lowerBone, Transform endBone)
    {
        Vector3 upperDirection = (lowerBone.position - upperBone.position).normalized;
        Vector3 lowerDirection = (endBone.position - lowerBone.position).normalized;

        if (upperDirection != Vector3.zero)
        {
            upperBone.rotation = Quaternion.LookRotation(upperDirection);
        }

        if (lowerDirection != Vector3.zero)
        {
            lowerBone.rotation = Quaternion.LookRotation(lowerDirection);
        }
    }

    Vector3 Map2Dto3D(Vector2 point, float depth)
    {
        return new Vector3(
            point.x * depthScale,
            yOffset + (imageHeight - point.y) * heightScale,
            depth
        );
    }

    void LoadCSV()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "keypoints.csv");
        if (!File.Exists(path))
        {
            Debug.LogError("CSV not found: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        var frames = lines.Skip(1).Where(line => line.Contains("squats_1")).ToArray();

        foreach (string frameData in frames)
        {
            string[] p = frameData.Split(',');
            if (p.Length < 44) continue;

            try
            {
                // Parse essential keypoints
                Vector2
                    leftShoulder = new Vector2(float.Parse(p[8]), float.Parse(p[9])),
                    rightShoulder = new Vector2(float.Parse(p[11]), float.Parse(p[12])),
                    leftElbow = new Vector2(float.Parse(p[14]), float.Parse(p[15])),
                    rightElbow = new Vector2(float.Parse(p[17]), float.Parse(p[18])),
                    leftWrist = new Vector2(float.Parse(p[20]), float.Parse(p[21])),
                    rightWrist = new Vector2(float.Parse(p[23]), float.Parse(p[24])),
                    leftHip = new Vector2(float.Parse(p[26]), float.Parse(p[27])),
                    rightHip = new Vector2(float.Parse(p[29]), float.Parse(p[30])),
                    leftKnee = new Vector2(float.Parse(p[32]), float.Parse(p[33])),
                    rightKnee = new Vector2(float.Parse(p[35]), float.Parse(p[36])),
                    leftAnkle = new Vector2(float.Parse(p[38]), float.Parse(p[39])),
                    rightAnkle = new Vector2(float.Parse(p[41]), float.Parse(p[42]));

                // Calculate depth from hips
                Vector2 hips2D = (leftHip + rightHip) / 2f;
                float depth = zOffset + (hips2D.y / imageHeight);

                // Head position (average of shoulders with vertical offset)
                Vector2 head2D = (leftShoulder + rightShoulder) / 2f;
                head2D.y -= 50f; // Adjust based on character

                Vector3[] frame = new Vector3[14];
                frame[0] = Map2Dto3D(hips2D, depth);        // Hips position
                frame[1] = Map2Dto3D(leftShoulder, depth);  // Left shoulder
                frame[2] = Map2Dto3D(rightShoulder, depth); // Right shoulder
                frame[3] = Map2Dto3D(leftElbow, depth);     // Left elbow
                frame[4] = Map2Dto3D(rightElbow, depth);    // Right elbow
                frame[5] = Map2Dto3D(leftWrist, depth);     // Left wrist
                frame[6] = Map2Dto3D(rightWrist, depth);    // Right wrist
                frame[7] = Map2Dto3D(leftHip, depth);       // Left hip
                frame[8] = Map2Dto3D(rightHip, depth);      // Right hip
                frame[9] = Map2Dto3D(leftKnee, depth);      // Left knee
                frame[10] = Map2Dto3D(rightKnee, depth);    // Right knee
                frame[11] = Map2Dto3D(leftAnkle, depth);    // Left ankle
                frame[12] = Map2Dto3D(rightAnkle, depth);   // Right ankle
                frame[13] = Map2Dto3D(head2D, depth);       // Head position

                keypointsFrames.Add(frame);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Skipping frame: {e.Message}");
            }
        }
        Debug.Log($"Loaded {keypointsFrames.Count} frames");
    }
}