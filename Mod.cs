using System;
using System.ComponentModel;

namespace _7DaysToDieModManager
{
    /// <summary>
    /// Represents a mod with properties and functionality for change notification.
    /// </summary>
    public class Mod : INotifyPropertyChanged
    {
        // Backing fields for properties
        private bool _active;
        private string _name;

        /// <summary>
        /// Gets or sets the name of the mod.
        /// </summary>
        /// <value>The name of the mod.</value>
        public string Name
        {
            get => _name;
            set
            {
                // Only update and notify if the value has changed
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name)); // Notify that the Name property has changed
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the mod is active.
        /// </summary>
        /// <value><c>true</c> if the mod is active; otherwise, <c>false</c>.</value>
        public bool Active
        {
            get => _active;
            set
            {
                // Only update and notify if the value has changed
                if (_active != value)
                {
                    _active = value;
                    OnPropertyChanged(nameof(Active)); // Notify that the Active property has changed
                }
            }
        }

        /// <summary>
        /// Gets or sets the version of the mod.
        /// </summary>
        /// <value>The version of the mod.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the date when the mod was last updated.
        /// </summary>
        /// <value>The last updated date.</value>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the directory path where the mod is located.
        /// </summary>
        /// <value>The directory path.</value>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the description of the mod.
        /// </summary>
        /// <value>The description of the mod.</value>
        public string Description { get; set; }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            // Invoke the PropertyChanged event if there are subscribers
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
