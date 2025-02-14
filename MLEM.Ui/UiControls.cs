using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MLEM.Input;
using MLEM.Misc;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;

#if NET452
using MLEM.Extensions;
#endif

namespace MLEM.Ui {
    /// <summary>
    /// UiControls holds and manages all of the controls for a <see cref="UiSystem"/>.
    /// UiControls supports keyboard, mouse, gamepad and touch input using an underlying <see cref="InputHandler"/>.
    /// </summary>
    public class UiControls {

        /// <summary>
        /// The input handler that is used for querying input
        /// </summary>
        public readonly InputHandler Input;
        /// <summary>
        /// A list of <see cref="Keys"/>, <see cref="Buttons"/> and/or <see cref="MouseButton"/> that act as the buttons on the keyboard which perform the <see cref="Element.OnPressed"/> action.
        /// If the <see cref="ModifierKey.Shift"/> is held, these buttons perform <see cref="Element.OnSecondaryPressed"/>.
        /// </summary>
        public readonly Keybind KeyboardButtons = new Keybind().Add(Keys.Space).Add(Keys.Enter);
        /// <summary>
        /// AA <see cref="Keybind"/> that acts as the buttons on a gamepad that perform the <see cref="Element.OnPressed"/> action.
        /// </summary>
        public readonly Keybind GamepadButtons = new Keybind(Buttons.A);
        /// <summary>
        /// A <see cref="Keybind"/> that acts as the buttons on a gamepad that perform the <see cref="Element.OnSecondaryPressed"/> action.
        /// </summary>
        public readonly Keybind SecondaryGamepadButtons = new Keybind(Buttons.X);
        /// <summary>
        /// A <see cref="Keybind"/> that acts as the buttons that select a <see cref="Element"/> that is above the currently selected element.
        /// </summary>
        public readonly Keybind UpButtons = new Keybind().Add(Buttons.DPadUp).Add(Buttons.LeftThumbstickUp);
        /// <summary>
        /// A <see cref="Keybind"/> that acts as the buttons that select a <see cref="Element"/> that is below the currently selected element.
        /// </summary>
        public readonly Keybind DownButtons = new Keybind().Add(Buttons.DPadDown).Add(Buttons.LeftThumbstickDown);
        /// <summary>
        /// A <see cref="Keybind"/> that acts as the buttons that select a <see cref="Element"/> that is to the left of the currently selected element.
        /// </summary>
        public readonly Keybind LeftButtons = new Keybind().Add(Buttons.DPadLeft).Add(Buttons.LeftThumbstickLeft);
        /// <summary>
        /// A <see cref="Keybind"/> that acts as the buttons that select a <see cref="Element"/> that is to the right of the currently selected element.
        /// </summary>
        public readonly Keybind RightButtons = new Keybind().Add(Buttons.DPadRight).Add(Buttons.LeftThumbstickRight);
        /// <summary>
        /// All <see cref="Keybind"/> instances used by these ui controls.
        /// This can be used to easily serialize and deserialize all ui keybinds.
        /// </summary>
        public readonly Keybind[] Keybinds;

        /// <summary>
        /// The <see cref="RootElement"/> that is currently active.
        /// The active root element is the one with the highest <see cref="RootElement.Priority"/> that <see cref="RootElement.CanBeActive"/>.
        /// </summary>
        public RootElement ActiveRoot { get; protected set; }
        /// <summary>
        /// The <see cref="Element"/> that the mouse is currently over.
        /// </summary>
        public Element MousedElement { get; protected set; }
        /// <summary>
        /// The <see cref="Element"/> that is currently touched.
        /// </summary>
        public Element TouchedElement { get; protected set; }
        /// <summary>
        /// The element that is currently selected.
        /// This is the <see cref="RootElement.SelectedElement"/> of the <see cref="ActiveRoot"/>.
        /// </summary>
        public Element SelectedElement => this.GetSelectedElement(this.ActiveRoot);
        /// <summary>
        /// The zero-based index of the <see cref="GamePad"/> used for gamepad input.
        /// If this index is lower than 0, every connected gamepad will trigger input.
        /// </summary>
        public int GamepadIndex = -1;
        /// <summary>
        /// Set this to false to disable mouse input for these ui controls.
        /// Note that this does not disable mouse input for the underlying <see cref="InputHandler"/>.
        /// </summary>
        public bool HandleMouse = true;
        /// <summary>
        /// Set this to false to disable keyboard input for these ui controls.
        /// Note that this does not disable keyboard input for the underlying <see cref="InputHandler"/>.
        /// </summary>
        public bool HandleKeyboard = true;
        /// <summary>
        /// Set this to false to disable touch input for these ui controls.
        /// Note that this does not disable touch input for the underlying <see cref="InputHandler"/>.
        /// </summary>
        public bool HandleTouch = true;
        /// <summary>
        /// Set this to false to disable gamepad input for these ui controls.
        /// Note that this does not disable gamepad input for the underlying <see cref="InputHandler"/>.
        /// </summary>
        public bool HandleGamepad = true;
        /// <summary>
        /// If this value is true, the ui controls are in automatic navigation mode.
        /// This means that the <see cref="UiStyle.SelectionIndicator"/> will be drawn around the <see cref="SelectedElement"/>.
        /// </summary>
        public bool IsAutoNavMode {
            get => this.isAutoNavMode;
            set {
                if (this.isAutoNavMode != value) {
                    this.isAutoNavMode = value;
                    this.AutoNavModeChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// An event that is raised when <see cref="IsAutoNavMode"/> is changed.
        /// This can be used for custom actions like hiding the mouse cursor when automatic navigation is enabled.
        /// </summary>
        public event Action<bool> AutoNavModeChanged;

        /// <summary>
        /// This value ist true if the <see cref="InputHandler"/> was created by this ui controls instance, or if it was passed in.
        /// If the input handler was created by this instance, its <see cref="InputHandler.Update()"/> method should be called by us.
        /// </summary>
        protected readonly bool IsInputOurs;
        /// <summary>
        /// The <see cref="UiSystem"/> that this ui controls instance is controlling
        /// </summary>
        protected readonly UiSystem System;

        private readonly Dictionary<string, Element> selectedElements = new Dictionary<string, Element>();
        private bool isAutoNavMode;

        /// <summary>
        /// Creates a new instance of the ui controls.
        /// You should rarely have to invoke this manually, since the <see cref="UiSystem"/> handles it.
        /// </summary>
        /// <param name="system">The ui system to control with these controls</param>
        /// <param name="inputHandler">The input handler to use for controlling, or null to create a new one.</param>
        public UiControls(UiSystem system, InputHandler inputHandler = null) {
            this.System = system;
            this.Input = inputHandler ?? new InputHandler(system.Game);
            this.IsInputOurs = inputHandler == null;
            this.Keybinds = typeof(UiControls).GetFields()
                .Where(f => f.FieldType == typeof(Keybind))
                .Select(f => (Keybind) f.GetValue(this)).ToArray();

            // enable all required gestures
            InputHandler.EnableGestures(GestureType.Tap, GestureType.Hold);
        }

        /// <summary>
        /// Update this ui controls instance, causing the underlying <see cref="InputHandler"/> to be updated, as well as ui input to be queried.
        /// </summary>
        public virtual void Update() {
            if (this.IsInputOurs)
                this.Input.Update();
            this.ActiveRoot = this.System.GetRootElements().FirstOrDefault(root => root.CanBeActive);

            if (this.HandleMouse) {
                var mousedNow = this.GetElementUnderPos(new Vector2(this.Input.ViewportMousePosition.X, this.Input.ViewportMousePosition.Y));
                this.SetMousedElement(mousedNow);

                if (this.Input.IsPressedAvailable(MouseButton.Left)) {
                    this.IsAutoNavMode = false;
                    var selectedNow = mousedNow != null && mousedNow.CanBeSelected ? mousedNow : null;
                    this.SelectElement(this.ActiveRoot, selectedNow);
                    if (mousedNow != null && mousedNow.CanBePressed) {
                        this.PressElement(mousedNow);
                        this.Input.TryConsumePressed(MouseButton.Left);
                    }
                } else if (this.Input.IsPressedAvailable(MouseButton.Right)) {
                    this.IsAutoNavMode = false;
                    if (mousedNow != null && mousedNow.CanBePressed) {
                        this.PressElement(mousedNow, true);
                        this.Input.TryConsumePressed(MouseButton.Right);
                    }
                }
            } else {
                this.SetMousedElement(null);
            }

            if (this.HandleKeyboard) {
                if (this.KeyboardButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.SelectedElement?.Root != null && this.SelectedElement.CanBePressed) {
                        // primary or secondary action on element using space or enter
                        this.PressElement(this.SelectedElement, this.Input.IsModifierKeyDown(ModifierKey.Shift));
                        this.KeyboardButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                    }
                } else if (this.Input.IsPressedAvailable(Keys.Tab)) {
                    this.IsAutoNavMode = true;
                    // tab or shift-tab to next or previous element
                    var backward = this.Input.IsModifierKeyDown(ModifierKey.Shift);
                    var next = this.GetTabNextElement(backward);
                    if (this.SelectedElement?.Root != null)
                        next = this.SelectedElement.GetTabNextElement(backward, next);
                    if (next != this.SelectedElement) {
                        this.SelectElement(this.ActiveRoot, next);
                        this.Input.TryConsumePressed(Keys.Tab);
                    }
                }
            }

            if (this.HandleTouch) {
                if (this.Input.GetViewportGesture(GestureType.Tap, out var tap)) {
                    this.IsAutoNavMode = false;
                    var tapped = this.GetElementUnderPos(tap.Position);
                    this.SelectElement(this.ActiveRoot, tapped);
                    if (tapped != null && tapped.CanBePressed)
                        this.PressElement(tapped);
                } else if (this.Input.GetViewportGesture(GestureType.Hold, out var hold)) {
                    this.IsAutoNavMode = false;
                    var held = this.GetElementUnderPos(hold.Position);
                    this.SelectElement(this.ActiveRoot, held);
                    if (held != null && held.CanBePressed)
                        this.PressElement(held, true);
                } else if (this.Input.ViewportTouchState.Count <= 0) {
                    this.SetTouchedElement(null);
                } else {
                    foreach (var location in this.Input.ViewportTouchState) {
                        var element = this.GetElementUnderPos(location.Position);
                        if (location.State == TouchLocationState.Pressed) {
                            // start touching an element if we just touched down on it
                            this.SetTouchedElement(element);
                        } else if (element != this.TouchedElement) {
                            // if we moved off of the touched element, we stop touching
                            this.SetTouchedElement(null);
                        }
                    }
                }
            } else {
                this.SetTouchedElement(null);
            }

            if (this.HandleGamepad) {
                if (this.GamepadButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.SelectedElement?.Root != null && this.SelectedElement.CanBePressed) {
                        this.PressElement(this.SelectedElement);
                        this.GamepadButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                    }
                } else if (this.SecondaryGamepadButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.SelectedElement?.Root != null && this.SelectedElement.CanBePressed) {
                        this.PressElement(this.SelectedElement, true);
                        this.SecondaryGamepadButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                    }
                } else if (this.DownButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.HandleGamepadNextElement(Direction2.Down))
                        this.DownButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                } else if (this.LeftButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.HandleGamepadNextElement(Direction2.Left))
                        this.LeftButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                } else if (this.RightButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.HandleGamepadNextElement(Direction2.Right))
                        this.RightButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                } else if (this.UpButtons.IsPressedAvailable(this.Input, this.GamepadIndex)) {
                    if (this.HandleGamepadNextElement(Direction2.Up))
                        this.UpButtons.TryConsumePressed(this.Input, this.GamepadIndex);
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="Element"/> in the underlying <see cref="UiSystem"/> that is currently below the given position.
        /// Throughout the ui system, this is used for mouse input querying.
        /// </summary>
        /// <param name="position">The position to query</param>
        /// <returns>The element under the position, or null if there isn't one</returns>
        public virtual Element GetElementUnderPos(Vector2 position) {
            foreach (var root in this.System.GetRootElements()) {
                var pos = Vector2.Transform(position, root.InvTransform);
                var moused = root.Element.GetElementUnderPos(pos);
                if (moused != null)
                    return moused;
            }
            return null;
        }

        /// <summary>
        /// Selects the given element that is a child of the given root element.
        /// Optionally, automatic navigation can be forced on, causing the <see cref="UiStyle.SelectionIndicator"/> to be drawn around the element.
        /// A simpler version of this method is <see cref="RootElement.SelectElement"/>.
        /// </summary>
        /// <param name="root">The root element of the <see cref="Element"/></param>
        /// <param name="element">The element to select, or null to deselect the selected element.</param>
        /// <param name="autoNav">Whether automatic navigation should be forced on</param>
        public void SelectElement(RootElement root, Element element, bool? autoNav = null) {
            if (root == null)
                return;
            if (element != null && !element.CanBeSelected)
                return;
            var selected = this.GetSelectedElement(root);
            if (selected == element)
                return;

            if (selected != null)
                this.System.InvokeOnElementDeselected(selected);
            if (element != null) {
                this.System.InvokeOnElementSelected(element);
                this.selectedElements[root.Name] = element;
            } else {
                this.selectedElements.Remove(root.Name);
            }
            this.System.InvokeOnSelectedElementChanged(element);

            if (autoNav != null)
                this.IsAutoNavMode = autoNav.Value;
        }

        /// <summary>
        /// Sets the <see cref="MousedElement"/> to the given value, calling the appropriate events.
        /// </summary>
        /// <param name="element">The element to set as moused</param>
        public void SetMousedElement(Element element) {
            if (element != null && !element.CanBeMoused)
                return;
            if (element != this.MousedElement) {
                if (this.MousedElement != null)
                    this.System.InvokeOnElementMouseExit(this.MousedElement);
                if (element != null)
                    this.System.InvokeOnElementMouseEnter(element);
                this.MousedElement = element;
                this.System.InvokeOnMousedElementChanged(element);
            }
        }

        /// <summary>
        /// Sets the <see cref="TouchedElement"/> to the given value, calling the appropriate events.
        /// </summary>
        /// <param name="element">The element to set as touched</param>
        public void SetTouchedElement(Element element) {
            if (element != null && !element.CanBeMoused)
                return;
            if (element != this.TouchedElement) {
                if (this.TouchedElement != null)
                    this.System.InvokeOnElementTouchExit(this.TouchedElement);
                if (element != null)
                    this.System.InvokeOnElementTouchEnter(element);
                this.TouchedElement = element;
                this.System.InvokeOnTouchedElementChanged(element);
            }
        }

        /// <summary>
        /// Returns the selected element for the given root element.
        /// A property equivalent to this method is <see cref="RootElement.SelectedElement"/>.
        /// </summary>
        /// <param name="root">The root element whose selected element to return</param>
        /// <returns>The given root's selected element, or null if the root doesn't exist, or if there is no selected element for that root.</returns>
        public Element GetSelectedElement(RootElement root) {
            if (root == null)
                return null;
            this.selectedElements.TryGetValue(root.Name, out var element);
            return element;
        }

        /// <summary>
        /// Causes the passed element to be pressed, invoking its <see cref="Element.OnPressed"/> or <see cref="Element.OnSecondaryPressed"/> through <see cref="UiSystem.OnElementPressed"/> or <see cref="UiSystem.OnElementSecondaryPressed"/>.
        /// </summary>
        /// <param name="element">The element to press.</param>
        /// <param name="secondary">Whether the secondary action should be invoked, rather than the primary one.</param>
        public void PressElement(Element element, bool secondary = false) {
            if (secondary) {
                this.System.InvokeOnElementSecondaryPressed(element);
            } else {
                this.System.InvokeOnElementPressed(element);
            }
        }

        /// <summary>
        /// Returns the next element to select when pressing the <see cref="Keys.Tab"/> key during keyboard navigation.
        /// If the <c>backward</c> boolean is true, the previous element should be returned instead.
        /// </summary>
        /// <param name="backward">If we're going backwards (if <see cref="ModifierKey.Shift"/> is held)</param>
        /// <returns>The next or previous element to select</returns>
        protected virtual Element GetTabNextElement(bool backward) {
            if (this.ActiveRoot == null)
                return null;
            var children = this.ActiveRoot.Element.GetChildren(c => !c.IsHidden, true, true).Append(this.ActiveRoot.Element)
                // we can't add these checks to GetChildren because it ignores false grandchildren
                .Where(c => c.CanBeSelected && c.AutoNavGroup == this.SelectedElement?.AutoNavGroup);
            if (this.SelectedElement?.Root != this.ActiveRoot) {
                // if we don't have an element selected in this root, navigate to the first one without a group
                var allowed = children.Where(c => c.AutoNavGroup == null);
                return backward ? allowed.LastOrDefault() : allowed.FirstOrDefault();
            } else {
                var foundCurr = false;
                Element lastFound = null;
                foreach (var child in children) {
                    if (child == this.SelectedElement) {
                        // when going backwards, return the last element found before the current one
                        if (backward)
                            return lastFound;
                        foundCurr = true;
                    } else {
                        // when going forwards, return the element after the current one
                        if (!backward && foundCurr)
                            return child;
                    }
                    lastFound = child;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the next element that should be selected during gamepad navigation, based on the <see cref="Direction2"/> that we're looking for elements in.
        /// </summary>
        /// <param name="direction">The direction that we're looking for next elements in</param>
        /// <returns>The first element found in that area</returns>
        protected virtual Element GetGamepadNextElement(Direction2 direction) {
            if (this.ActiveRoot == null)
                return null;
            var children = this.ActiveRoot.Element.GetChildren(c => !c.IsHidden, true, true).Append(this.ActiveRoot.Element)
                // we can't add these checks to GetChildren because it ignores false grandchildren
                .Where(c => c.CanBeSelected && c.AutoNavGroup == this.SelectedElement?.AutoNavGroup);
            if (this.SelectedElement?.Root != this.ActiveRoot) {
                // if we don't have an element selected in this root, navigate to the first one without a group
                return children.FirstOrDefault(c => c.AutoNavGroup == null);
            } else {
                Element closest = null;
                float closestPriority = 0;
                foreach (var child in children) {
                    if (child == this.SelectedElement)
                        continue;
                    var offset = child.Area.Center - this.SelectedElement.Area.Center;
                    var angle = Math.Abs(MathHelper.WrapAngle(direction.Angle() - (float) Math.Atan2(offset.Y, offset.X)));
                    if (angle >= MathHelper.PiOver2 - Element.Epsilon)
                        continue;
                    var distSq = child.Area.DistanceSquared(this.SelectedElement.Area);
                    // both distance and angle play a role in a destination button's priority, so we combine them
                    var priority = (distSq + 1) * (angle / MathHelper.PiOver2 + 1);
                    if (closest == null || priority < closestPriority) {
                        closest = child;
                        closestPriority = priority;
                    }
                }
                return closest;
            }
        }

        private bool HandleGamepadNextElement(Direction2 dir) {
            this.IsAutoNavMode = true;
            var next = this.GetGamepadNextElement(dir);
            if (this.SelectedElement != null)
                next = this.SelectedElement.GetGamepadNextElement(dir, next);
            if (next != null) {
                this.SelectElement(this.ActiveRoot, next);
                return true;
            }
            return false;
        }

    }
}
