﻿using System;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MSR.Application.Base.Model
{
    public abstract class TrackableModel : ViewModelBase, IChangeTracking
    {
        #region Global Variables / Properties

        private bool _isChanged;

        private List<KeyValuePair<string, object>> _databaseValues;

        private bool _changeTracking;

    #endregion

    #region Constructor

    protected TrackableModel()
        {
            _changeTracking = false;
            PropertyChanged += new PropertyChangedEventHandler(OnNotifiedOfPropertyChanged);
            _databaseValues = new List<KeyValuePair<string, object>>();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Get the database value (if any) with the provided property expression.
        /// </summary>
        protected object GetDatabaseValue<T>(Expression<Func<T>> propertyExpression)
        {
            return GetDatabaseValue(GetPropertyName(propertyExpression));
        }

        /// <summary>
        /// Get the database value (if any) with the provided property name.
        /// </summary>
        protected object GetDatabaseValue(string propertyName)
        {
            var databaseValue = _databaseValues.SingleOrDefault(x => x.Key == propertyName);

            return databaseValue.Value;
        }

        /// <summary>
        /// Get the property value with the provided property expression.
        /// </summary>
        protected object GetPropertyValue<T>(Expression<Func<T>> propertyExpression)
        {
            return GetPropertyValue(GetPropertyName(propertyExpression));
        }

        /// <summary>
        /// Get the property value with the provided property name.
        /// </summary>
        protected object GetPropertyValue(string propertyName)
        {
            var propertyInfo = GetType().GetProperty(propertyName);

            return propertyInfo.GetValue(this);
        }

        /// <summary>
        /// Set the database value for the property with the provided name.
        /// </summary>
        protected void SetDatabaseValue(string propertyName, object propertyValue)
        {
            //See if the database value has been set yet.
            var index = _databaseValues.FindIndex(x => x.Key == propertyName);

            if (index == -1)
            {
                //Property value has not been added yet to the list of database values. So add it.
                _databaseValues.Add(new KeyValuePair<string, object>(propertyName, propertyValue));
                return;
            }

            //Property value has already been added so update it.
            _databaseValues[index] = new KeyValuePair<string, object>(propertyName, propertyValue);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the IsChanged value to true if a property has changed.
        /// </summary>
        private void OnNotifiedOfPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null && !string.Equals(e.PropertyName, "IsChanged", StringComparison.Ordinal))
            {
                if (_changeTracking)
                {
                    //If we have allowed change tracking then set the IsChanged field to true and return.
                    IsChanged = true;
                    return;
                }

                //If change tracking is not enabled then we must be still populating the inital model and have not enabled change tracking.
                var propertyValue = GetPropertyValue(e.PropertyName);

                //Set the database value.
                SetDatabaseValue(e.PropertyName, propertyValue);
            }
        }

        #endregion

        #region Implemented IChangeTracking Members

        /// <summary>
        /// Track if the model has changed.
        /// </summary>
        public bool IsChanged
        {
            get
            {
                return _isChanged;
            }
            protected set
            {
                if (value == _isChanged) return;
                _isChanged = value;
                RaisePropertyChanged("IsChanged");
            }
        }

        /// <summary>
        /// Return true or false if the provided property has changed from it's database value.
        /// </summary>
        public bool HasChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (!_changeTracking) throw new Exception("Change tracking is not enabled");

            //Get the database value.
            var databaseValue = GetDatabaseValue(propertyExpression);

            //Get the property value.
            var propertyValue = GetPropertyValue(propertyExpression);

            //With json serialization compare the values to detect changes.
            return Newtonsoft.Json.JsonConvert.SerializeObject(databaseValue) !=
                Newtonsoft.Json.JsonConvert.SerializeObject(propertyValue);
        }

        /// <summary>
        /// Resets the model's state to unchanged by accepting the modifications.
        /// </summary>
        public void AcceptChanges()
        {
            if (!_changeTracking) throw new Exception("Change tracking is not enabled");

            //Sync the changes in each tracked property to the database values.
            for (var i = 0; i < _databaseValues.Count; i++)
            {
                var propertyValue = GetPropertyValue(_databaseValues[i].Key);
                _databaseValues[i] = new KeyValuePair<string, object>(_databaseValues[i].Key, propertyValue);
            }

            IsChanged = false;
        }

        /// <summary>
        /// Enable change tracking.
        /// </summary>
        public void EnableChangeTracking()
        {
            if (_changeTracking) throw new Exception("Change tracking is already enabled");

            _changeTracking = true;
        }

        #endregion
    }
}