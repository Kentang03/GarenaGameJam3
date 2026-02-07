#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class CharacterAnimatorBuilderWindow : EditorWindow
{
    [Header("Animation Clips")] 
    public AnimationClip idleClip;
    public AnimationClip moveClip; // shared walk/run clip
    public AnimationClip jumpClip; // optional

    [Header("Controller Output")] 
    public string controllerPath = "Assets/Character2D.controller";
    public bool assignToSelected = true;

    [Header("Jump Options")]
    public bool includeJump = true; // enable jump state & transitions
    public bool jumpFromAnyState = true; // use AnyState -> Jump on trigger
    public bool jumpReturnToLocomotion = true; // return to Locomotion when grounded; otherwise to Idle
    public bool jumpReturnOnGrounded = true; // return when IsGrounded == true; otherwise use exit time
    public float jumpTransitionDuration = 0f; // seconds
    public float jumpExitTimeNormalized = 0.8f; // used when return on exit time
    public bool jumpReturnWhenFalling = true; // require VelY <= threshold to return (prevents exiting while going up)
    public float fallingVelYThreshold = 0f; // typically 0: return only when starting to fall

    [Header("Locomotion Options")]
    public float idleToMoveDuration = 0.1f;
    public float moveToIdleDuration = 0.1f;

    [Header("Parameter Names (match Character2D)")]
    public string speedParam = "Speed";
    public string isRunningParam = "IsRunning";
    public string isGroundedParam = "IsGrounded";
    public string velYParam = "VelY";
    public string jumpTriggerParam = "Jump";
    public string animSpeedParam = "AnimSpeedMult";

    [Header("Anim Speed Multiplier")] 
    public float walkAnimSpeed = 1f;
    public float runAnimSpeed = 1.5f; // used by script; controller uses parameter

    [MenuItem("Tools/2D Character/Animator Builder")]
    public static void ShowWindow()
    {
        var win = GetWindow<CharacterAnimatorBuilderWindow>("Animator Builder");
        win.minSize = new Vector2(380, 320);
    }

    void OnGUI()
    {
        GUILayout.Label("Animation Clips", EditorStyles.boldLabel);
        idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle Clip", idleClip, typeof(AnimationClip), false);
        moveClip = (AnimationClip)EditorGUILayout.ObjectField("Move Clip (Walk/Run)", moveClip, typeof(AnimationClip), false);
        jumpClip = (AnimationClip)EditorGUILayout.ObjectField("Jump Clip (Optional)", jumpClip, typeof(AnimationClip), false);

        EditorGUILayout.Space();
        GUILayout.Label("Controller Output", EditorStyles.boldLabel);
        controllerPath = EditorGUILayout.TextField("Save Path", controllerPath);
        assignToSelected = EditorGUILayout.Toggle("Assign To Selected", assignToSelected);

        EditorGUILayout.Space();
        GUILayout.Label("Jump Options", EditorStyles.boldLabel);
        includeJump = EditorGUILayout.Toggle("Include Jump", includeJump);
        using (new EditorGUI.DisabledScope(!includeJump))
        {
            jumpFromAnyState = EditorGUILayout.Toggle("Jump From AnyState", jumpFromAnyState);
            jumpReturnToLocomotion = EditorGUILayout.Toggle("Return To Locomotion", jumpReturnToLocomotion);
            jumpReturnOnGrounded = EditorGUILayout.Toggle("Return On Grounded", jumpReturnOnGrounded);
            jumpReturnWhenFalling = EditorGUILayout.Toggle("Return When Falling (VelY)", jumpReturnWhenFalling);
            if (jumpReturnWhenFalling)
            {
                fallingVelYThreshold = EditorGUILayout.FloatField("Falling Threshold (VelY)", fallingVelYThreshold);
            }
            jumpTransitionDuration = EditorGUILayout.FloatField("Transition Duration", jumpTransitionDuration);
            if (!jumpReturnOnGrounded)
            {
                jumpExitTimeNormalized = EditorGUILayout.Slider("Exit Time (0-1)", jumpExitTimeNormalized, 0f, 1f);
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("Locomotion Options", EditorStyles.boldLabel);
        idleToMoveDuration = EditorGUILayout.FloatField("Idle → Locomotion Duration", idleToMoveDuration);
        moveToIdleDuration = EditorGUILayout.FloatField("Locomotion → Idle Duration", moveToIdleDuration);

        EditorGUILayout.Space();
        GUILayout.Label("Parameter Names", EditorStyles.boldLabel);
        speedParam = EditorGUILayout.TextField("Speed", speedParam);
        isRunningParam = EditorGUILayout.TextField("IsRunning", isRunningParam);
        isGroundedParam = EditorGUILayout.TextField("IsGrounded", isGroundedParam);
        velYParam = EditorGUILayout.TextField("VelY", velYParam);
        jumpTriggerParam = EditorGUILayout.TextField("Jump (Trigger)", jumpTriggerParam);
        animSpeedParam = EditorGUILayout.TextField("AnimSpeedMult", animSpeedParam);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create Animator Controller"))
        {
            CreateController();
        }
    }

    void CreateController()
    {
        if (moveClip == null)
        {
            EditorUtility.DisplayDialog("Missing Clip", "Please assign the Move clip.", "OK");
            return;
        }
        if (string.IsNullOrEmpty(controllerPath))
        {
            EditorUtility.DisplayDialog("Invalid Path", "Please provide a valid controller save path.", "OK");
            return;
        }

        // Ensure folder exists
        var folder = System.IO.Path.GetDirectoryName(controllerPath);
        if (!System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters
        controller.AddParameter(speedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(animSpeedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(velYParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(isGroundedParam, AnimatorControllerParameterType.Bool);
        controller.AddParameter(isRunningParam, AnimatorControllerParameterType.Bool);
        controller.AddParameter(jumpTriggerParam, AnimatorControllerParameterType.Trigger);

        // Layer and state machine
        var layer = controller.layers[0];
        var sm = layer.stateMachine;
        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];

        // States
        var idleState = sm.AddState("Idle");
        if (idleClip != null) idleState.motion = idleClip;
        sm.defaultState = idleState;

        var moveState = sm.AddState("Locomotion");
        moveState.motion = moveClip;
        moveState.speedParameter = animSpeedParam;
        moveState.speedParameterActive = true;

        AnimatorState jumpState = null;
        if (includeJump && jumpClip != null)
        {
            jumpState = sm.AddState("Jump");
            jumpState.motion = jumpClip;
        }

        // Transitions: Idle <-> Locomotion via IsRunning
        var idleToMove = idleState.AddTransition(moveState);
        idleToMove.hasExitTime = false;
        idleToMove.duration = idleToMoveDuration;
        idleToMove.AddCondition(AnimatorConditionMode.If, 0f, isRunningParam);

        var moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.hasExitTime = false;
        moveToIdle.duration = moveToIdleDuration;
        moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, isRunningParam);

        // Jump transitions
        if (jumpState != null)
        {
            if (jumpFromAnyState)
            {
                var anyToJump = sm.AddAnyStateTransition(jumpState);
                anyToJump.hasExitTime = false;
                anyToJump.duration = jumpTransitionDuration;
                anyToJump.AddCondition(AnimatorConditionMode.If, 0f, jumpTriggerParam);
            }
            else
            {
                var idleToJump = idleState.AddTransition(jumpState);
                idleToJump.hasExitTime = false;
                idleToJump.duration = jumpTransitionDuration;
                idleToJump.AddCondition(AnimatorConditionMode.If, 0f, jumpTriggerParam);

                var moveToJump = moveState.AddTransition(jumpState);
                moveToJump.hasExitTime = false;
                moveToJump.duration = jumpTransitionDuration;
                moveToJump.AddCondition(AnimatorConditionMode.If, 0f, jumpTriggerParam);
            }

            var returnTarget = jumpReturnToLocomotion ? moveState : idleState;
            var jumpReturn = jumpState.AddTransition(returnTarget);
            jumpReturn.duration = jumpTransitionDuration;
            // Determine conditions vs exit time
            bool useConditions = jumpReturnOnGrounded || jumpReturnWhenFalling;
            jumpReturn.hasExitTime = !useConditions;
            if (useConditions)
            {
                if (jumpReturnOnGrounded)
                {
                    jumpReturn.AddCondition(AnimatorConditionMode.If, 0f, isGroundedParam);
                }
                if (jumpReturnWhenFalling)
                {
                    // Return only when vertical velocity is less than or equal to threshold (typically <= 0)
                    jumpReturn.AddCondition(AnimatorConditionMode.Less, fallingVelYThreshold, velYParam);
                }
            }
            else
            {
                jumpReturn.exitTime = jumpExitTimeNormalized;
            }
        }

        // Assign to selected Animator(s)
        if (assignToSelected)
        {
            var animators = Selection.GetFiltered<Animator>(SelectionMode.TopLevel | SelectionMode.Editable);
            foreach (var anim in animators)
            {
                anim.runtimeAnimatorController = controller;
            }
        }

        EditorUtility.DisplayDialog("Animator Created", $"Controller saved to:\n{controllerPath}", "OK");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
