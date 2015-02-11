﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using SPLConqueror_Core;

namespace MachineLearning.Learning
{
    public class ML_Settings
    {

        /// <summary>
        /// The learner can learn quadratic functions of one numeric option, without learning the linear function apriory, if this property is true.
        /// </summary>
        public bool quadraticFunctionSupport = true;

        /// <summary>
        /// Cross validation is used during learning process if this property is true. 
        /// </summary>
        public bool crossValidation = false;


        /// <summary>
        /// If true, the learn algorithm can learn logarithmic functions auch as log(soption1). 
        /// </summary>
        public bool learn_logFunction = false;

        /// <summary>
        /// Defines the number of rounds the learning process have to be performed. 
        /// </summary>
        public int numberOfRounds = 30;


        /// <summary>
        /// Returns a new settings object with the settings specified in the file as key value pair. Settings not beeing specified in this file will have the default value. 
        /// </summary>
        /// <param name="settingLocation">Full qualified name of the settings file.</param>
        /// <returns>A settings object with the values specified in the file.</returns>
        public static ML_Settings readSettings(string settingLocation)
        {
            ML_Settings mls = new ML_Settings();

            System.IO.StreamReader file = new System.IO.StreamReader(settingLocation);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                string[] nameAndValue = line.Split(new char[]{' '},2);
                if (!mls.setSetting(nameAndValue[0], nameAndValue[1]))
                {
                    ErrorLog.logError("MlSetting " + nameAndValue[0] + " not found!");
                }
            }
            file.Close();

            return mls;
        }

        public ML_Settings()
        {
        }


        /// <summary>
        /// Set the value of one property of this object.
        /// </summary>
        /// <param name="name">Name of the field to be set.</param>
        /// <param name="value">String representation of the value of the field.</param>
        /// <returns>True of the field could be set with the given value. False if there is no field with the given name.</returns>
        public bool setSetting(string name, string value)
        {
            System.Reflection.FieldInfo fi =  this.GetType().GetField(name);
 
            if (fi == null)
                return false;

            if (fi.FieldType.FullName.Equals("System.Boolean"))
            {
                fi.SetValue(this, Convert.ToBoolean(value));
                return true;
            }
            if(fi.FieldType.FullName.Equals("System.Int32"))
            {
                fi.SetValue(this, Convert.ToInt32(value));
                return true;
            }
            if (fi.FieldType.FullName.Equals("System.Int64"))
            {
                fi.SetValue(this, Convert.ToInt64(value));
                return true;
            }
            return false;
        }


        /// <summary>
        /// A textual representation of the machine learning settings. The representation consist of a key value representation of all field of the settings with the dedicated values. 
        /// </summary>
        /// <returns>textual representation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            FieldInfo[] fields = this.GetType().GetFields();

            foreach (FieldInfo field in fields)
            {
                if(!field.IsStatic)
                    sb.Append(field.Name+" : "+field.GetValue(this)+System.Environment.NewLine);
            }
 	        return sb.ToString();
        }


    }
}