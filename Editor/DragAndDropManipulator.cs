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
            target.RegisterCallback<DragEnterEvent>(OnDragEnter, TrickleDown.TrickleDown);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeave, TrickleDown.TrickleDown);

            target.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
            target.RegisterCallback<DragExitedEvent>(OnDragExited, TrickleDown.TrickleDown);
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
                if (Utils.IsPrefab(objects[i])) { _valid = true; }
            }
            if (_valid)
            {
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }

            UpdateVisualMode();
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            UpdateVisualMode();
            if (_valid)
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            UpdateVisualMode();
            if (_valid)
            {
                evt.StopImmediatePropagation();
                target.ReleaseMouse();
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (_valid)
            {
                Object[] objects = DragAndDrop.objectReferences;
                List<GameObject> gameObjects = new List<GameObject>();
                for (int i = 0; i < objects.Length; i++)
                {
                    if (Utils.IsPrefab(objects[i])) { gameObjects.Add(objects[i] as GameObject); }
                }

                _onDragPerform?.Invoke(gameObjects);
                DragAndDrop.AcceptDrag();

                evt.StopImmediatePropagation();
                target.ReleaseMouse();
            }
            _valid = false;

            UpdateVisualMode();
        }

        private void OnDragExited(DragExitedEvent evt)
        {
            if (_valid)
            {
                evt.StopImmediatePropagation();
                target.ReleaseMouse();
            }
            _valid = false;
            UpdateVisualMode();
        }

        private void UpdateVisualMode()
        {
            DragAndDrop.visualMode = _valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
        }

    }
}
