using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Selection;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The Examine System allows additional detail of items to be displayed when
    /// the cursor hovers over them. The particular information displayed is item
    /// and requirement dependant, and may take different formats.
    /// </summary>
    public class ExamineSystem : NetworkSystem
    {
        public delegate void ExaminableChangedHandler(AbstractExaminable examinable);

        public event ExaminableChangedHandler OnExaminableChanged;

        private SelectionSystem _selectionSystem;

        protected override void OnAwake()
        {
            base.OnAwake();
            _selectionSystem = Subsystems.Get<SelectionSystem>();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            if (_selectionSystem)
            {
                _selectionSystem.OnSelectableChanged += UpdateExaminable;
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            if (_selectionSystem)
            {
                _selectionSystem.OnSelectableChanged -= UpdateExaminable;
            }
        }

        private void UpdateExaminable()
        {
            // Get the examinable under the cursor
            AbstractExaminable current = _selectionSystem.GetCurrentSelectable<AbstractExaminable>();
            OnExaminableChanged?.Invoke(current);
        }
    }
}
