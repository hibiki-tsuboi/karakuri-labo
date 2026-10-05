using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private GoalController goal;
        [SerializeField] private UIManager ui;
        [SerializeField] private PhysicsObject[] resetObjects = Array.Empty<PhysicsObject>();
        [SerializeField] private StageObjective objective;

        private GoalController subscribedGoal;
        private readonly List<PhysicsObject> capturedObjects = new List<PhysicsObject>();

        public GameState State { get; private set; } = GameState.Edit;
        public bool HasReachedGoal { get; private set; }
        public AttemptFailure FailureReason { get; private set; }
        public event Action<GameState> StateChanged;

        private void OnEnable()
        {
            SubscribeToGoal();
        }

        private void OnDisable()
        {
            UnsubscribeFromGoal();
        }

        private void Update()
        {
            if (State == GameState.Playing && HasReachedGoal)
            {
                TryCompleteStage();
            }
        }

        public void ConfigureObjective(StageObjective stageObjective)
        {
            if (State == GameState.Edit)
            {
                objective = stageObjective;
                if (objective != null)
                {
                    objective.ResetProgress();
                }
            }
        }

        public void Configure(BallController stageBall, GoalController stageGoal, UIManager stageUi)
        {
            if (State != GameState.Edit)
            {
                Debug.LogError("Configure the stage before starting its simulation.", this);
                return;
            }

            UnsubscribeFromGoal();
            ball = stageBall;
            goal = stageGoal;
            ui = stageUi;

            if (ui != null && ui.IsConfigured)
            {
                ui.SetCleared(false);
            }

            if (isActiveAndEnabled)
            {
                SubscribeToGoal();
            }
        }

        public void ConfigureResetObjects(params PhysicsObject[] objects)
        {
            if (State != GameState.Edit)
            {
                Debug.LogError("Configure reset objects before starting the simulation.", this);
                return;
            }

            resetObjects = Array.Empty<PhysicsObject>();
            RegisterResetObjects(objects);
        }

        public bool RegisterResetObjects(params PhysicsObject[] objects)
        {
            if (State != GameState.Edit)
            {
                return false;
            }

            var registered = new List<PhysicsObject>();
            foreach (PhysicsObject physicsObject in resetObjects)
            {
                AddUnique(registered, physicsObject);
            }
            if (objects != null)
            {
                foreach (PhysicsObject physicsObject in objects)
                {
                    AddUnique(registered, physicsObject);
                }
            }

            // Moving a restored parent also moves its children. Always restore
            // assembly roots before their independently simulated descendants.
            registered.Sort((left, right) => HierarchyDepth(left.transform)
                .CompareTo(HierarchyDepth(right.transform)));
            resetObjects = registered.ToArray();
            return true;
        }

        public bool UnregisterResetObjects(params PhysicsObject[] objects)
        {
            if (State != GameState.Edit)
            {
                return false;
            }

            var removed = objects != null
                ? new HashSet<PhysicsObject>(objects)
                : new HashSet<PhysicsObject>();
            var registered = new List<PhysicsObject>();
            foreach (PhysicsObject physicsObject in resetObjects)
            {
                if (physicsObject != null && !removed.Contains(physicsObject))
                {
                    AddUnique(registered, physicsObject);
                }
            }
            resetObjects = registered.ToArray();
            capturedObjects.RemoveAll(physicsObject => physicsObject == null || removed.Contains(physicsObject));
            return true;
        }

        private static void AddUnique(List<PhysicsObject> objects, PhysicsObject physicsObject)
        {
            if (physicsObject != null && !objects.Contains(physicsObject))
            {
                objects.Add(physicsObject);
            }
        }

        private static int HierarchyDepth(Transform target)
        {
            int depth = 0;
            while (target.parent != null)
            {
                depth++;
                target = target.parent;
            }
            return depth;
        }

        [ContextMenu("Start Simulation")]
        public void StartSimulation()
        {
            if (State != GameState.Edit)
            {
                return;
            }

            if (ball == null || goal == null || ui == null || !ui.IsConfigured)
            {
                Debug.LogError("GameManager requires Ball, Goal, and UI with a CLEAR text reference.", this);
                return;
            }

            SubscribeToGoal();
            CaptureState();
            ui.SetCleared(false);
            State = GameState.Playing;
            HasReachedGoal = false;
            FailureReason = AttemptFailure.None;
            if (objective != null)
            {
                objective.BeginAttempt();
            }
            foreach (PhysicsObject physicsObject in capturedObjects)
            {
                if (physicsObject != null)
                {
                    physicsObject.BeginSimulation();
                }
            }

            ball.BeginSimulation();
            StateChanged?.Invoke(State);
        }

        [ContextMenu("Start Simulation", true)]
        private bool CanStartSimulation()
        {
            return Application.isPlaying && State == GameState.Edit;
        }

        [ContextMenu("Reset Simulation")]
        public void ResetSimulation()
        {
            if (State == GameState.Edit)
            {
                return;
            }

            // Ignore any goal notification during restoration, then notify the HUD
            // and placement controls only after every object is back in place.
            State = GameState.Edit;
            HasReachedGoal = false;
            FailureReason = AttemptFailure.None;
            if (objective != null)
            {
                objective.ResetProgress();
            }
            foreach (PhysicsObject physicsObject in capturedObjects)
            {
                if (physicsObject != null)
                {
                    physicsObject.RestoreState();
                }
            }

            Physics.SyncTransforms();
            if (ui != null && ui.IsConfigured)
            {
                ui.SetCleared(false);
            }

            StateChanged?.Invoke(State);
        }

        [ContextMenu("Reset Simulation", true)]
        private bool CanResetSimulation()
        {
            return Application.isPlaying && State != GameState.Edit;
        }

        public bool FailAttempt(AttemptFailure reason)
        {
            if (State != GameState.Playing || reason == AttemptFailure.None)
            {
                return false;
            }

            // A goal reached on the last physics step takes priority over failure.
            TryCompleteStage();
            if (State != GameState.Playing)
            {
                return false;
            }

            State = GameState.Failed;
            FailureReason = reason;
            foreach (PhysicsObject physicsObject in capturedObjects)
            {
                if (physicsObject != null)
                {
                    physicsObject.FreezeSimulation();
                }
            }
            ui.SetCleared(false);
            StateChanged?.Invoke(State);
            return true;
        }

        public bool RetryAttempt()
        {
            if (State != GameState.Failed)
            {
                return false;
            }
            ResetSimulation();
            StartSimulation();
            return State == GameState.Playing;
        }

        private void CaptureState()
        {
            capturedObjects.Clear();

            // RequireComponent covers new balls; this also supports older scene
            // instances created before the shared snapshot component was added.
            PhysicsObject ballPhysics = ball.GetComponent<PhysicsObject>();
            if (ballPhysics == null)
            {
                ballPhysics = ball.gameObject.AddComponent<PhysicsObject>();
            }

            CaptureObject(ballPhysics);
            foreach (PhysicsObject physicsObject in resetObjects)
            {
                CaptureObject(physicsObject);
            }
        }

        private void CaptureObject(PhysicsObject physicsObject)
        {
            if (physicsObject == null || capturedObjects.Contains(physicsObject))
            {
                return;
            }

            physicsObject.CaptureState();
            capturedObjects.Add(physicsObject);
        }

        private void SubscribeToGoal()
        {
            if (subscribedGoal == goal)
            {
                return;
            }

            UnsubscribeFromGoal();
            subscribedGoal = goal;
            if (subscribedGoal != null)
            {
                subscribedGoal.BallEntered += HandleBallEntered;
            }
        }

        private void UnsubscribeFromGoal()
        {
            if (subscribedGoal != null)
            {
                subscribedGoal.BallEntered -= HandleBallEntered;
            }

            subscribedGoal = null;
        }

        private void HandleBallEntered(BallController enteredBall)
        {
            if (State != GameState.Playing || enteredBall != ball)
            {
                return;
            }

            HasReachedGoal = true;
            TryCompleteStage();
        }

        private void TryCompleteStage()
        {
            if (State != GameState.Playing || !HasReachedGoal ||
                (objective != null && !objective.IsComplete))
            {
                return;
            }

            State = GameState.Clear;
            if (ui != null)
            {
                ui.SetCleared(true);
            }

            StateChanged?.Invoke(State);
        }
    }
}
