using System;
using Microsoft.AspNetCore.Components;

namespace Libsrd5Blazor.Screens {

    /// <summary>
    /// Base class of the four screens (the Unity scenes). Registers itself as the receiver
    /// of number keys and maps Start / KeyPressHandler to Blazor's component lifecycle.
    /// </summary>
    public abstract class ScreenBase : ComponentBase, IKeyInputReceiver, IDisposable {
        protected bool disposed;

        protected override void OnInitialized() {
            Game.KeyReceiver = this;
            Start();
        }

        /// <summary>Unity's Start().</summary>
        protected virtual void Start() { }

        /// <summary>Handles a number key (or Return) and re-renders the screen.</summary>
        public void KeyPressHandler(KeyCode code) {
            if (disposed) return;
            OnKey(code);
            if (!disposed) StateHasChanged();
        }

        protected abstract void OnKey(KeyCode code);

        protected void LoadScene(string sceneName) {
            Game.LoadScene(sceneName);
        }

        public virtual void Dispose() {
            disposed = true;
            if (Game.KeyReceiver == this) Game.KeyReceiver = null;
        }
    }
}
