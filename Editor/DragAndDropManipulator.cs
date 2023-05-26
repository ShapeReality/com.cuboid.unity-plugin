using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Cuboid.UnityPlugin.Editor
{
    [DefaultExecutionOrder(1000)]
    public class DragAndDropManipulator : PointerManipulator
    {
        private Action<List<GameObject>> _onDragPerform;

        public DragAndDropManipulator(VisualElement target, Action<List<GameObject>> onDragPerform)
        {
            this.target = target;
            _onDragPerform = onDragPerform;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragEnterEvent>(OnDragEnter, TrickleDown.NoTrickleDown);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.NoTrickleDown);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeave, TrickleDown.NoTrickleDown);

            target.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.NoTrickleDown);
            target.RegisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.NoTrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);

            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            target.UnregisterCallback<DragExitedEvent>(OnDragExited);
        }

        private bool _valid = false;

        private void OnDragEnter(DragEnterEvent evt)
        {
            Object[] objects = DragAndDrop.objectReferences;

            _valid = false;
            for (int i = 0; i < objects.Length; i++)
            {
                if (IsPrefab(objects[i])) { _valid = true; }
            }

            UpdateVisualMode();
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            UpdateVisualMode();
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            _valid = false;
            UpdateVisualMode();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (_valid)
            {
                Object[] objects = DragAndDrop.objectReferences;
                List<GameObject> gameObjects = new List<GameObject>();
                for (int i = 0; i < objects.Length; i++)
                {
                    if (IsPrefab(objects[i])) { gameObjects.Add(objects[i] as GameObject); }
                }

                _onDragPerform?.Invoke(gameObjects);
                DragAndDrop.AcceptDrag();
            }
            UpdateVisualMode();
        }

        private void OnDragExited(DragExitedEvent evt)
        {
            UpdateVisualMode();
        }

        private void UpdateVisualMode()
        {
            DragAndDrop.visualMode = _valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        }

        private bool IsPrefab(Object obj)
        {
            GameObject gameObject = obj as GameObject;
            return gameObject != null && gameObject.scene.name == null && gameObject.scene.rootCount == 0;
        }
    }
}
