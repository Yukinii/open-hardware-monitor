﻿/*
  
  Version: MPL 1.1/GPL 2.0/LGPL 2.1

  The contents of this file are subject to the Mozilla Public License Version
  1.1 (the "License"); you may not use this file except in compliance with
  the License. You may obtain a copy of the License at
 
  http://www.mozilla.org/MPL/

  Software distributed under the License is distributed on an "AS IS" basis,
  WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
  for the specific language governing rights and limitations under the License.

  The Original Code is the Open Hardware Monitor code.

  The Initial Developer of the Original Code is 
  Michael Möller <m.moeller@gmx.ch>.
  Portions created by the Initial Developer are Copyright (C) 2009-2010
  the Initial Developer. All Rights Reserved.

  Contributor(s):

  Alternatively, the contents of this file may be used under the terms of
  either the GNU General Public License Version 2 or later (the "GPL"), or
  the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
  in which case the provisions of the GPL or the LGPL are applicable instead
  of those above. If you wish to allow use of your version of this file only
  under the terms of either the GPL or the LGPL, and not to allow others to
  use your version of this file under the terms of the MPL, indicate your
  decision by deleting the provisions above and replace them with the notice
  and other provisions required by the GPL or the LGPL. If you do not delete
  the provisions above, a recipient may use your version of this file under
  the terms of any one of the MPL, the GPL or the LGPL.
 
*/

using System;
using System.Collections.Generic;
using OpenHardwareMonitor.Collections;

namespace OpenHardwareMonitor.Hardware {

  internal class Sensor : ISensor {

    private string defaultName;
    private string name;
    private int index;
    private bool defaultHidden;
    private SensorType sensorType;
    private IHardware hardware;
    private ReadOnlyArray<IParameter> parameters;
    private float? value;
    private float? min;
    private float? max;
    private Queue<SensorValue> values =
      new Queue<SensorValue>(MAX_MINUTES * 15);
    private ISettings settings;
    
    private float sum = 0;
    private int count = 0;

    private const int MAX_MINUTES = 120;
   
    public Sensor(string name, int index, SensorType sensorType,
      IHardware hardware, ISettings settings) : 
      this(name, index, sensorType, hardware, null, settings) { }

    public Sensor(string name, int index, SensorType sensorType,
      IHardware hardware, ParameterDescription[] parameterDescriptions, 
      ISettings settings) :
      this(name, index, false, sensorType, hardware,
        parameterDescriptions, settings) { }

    public Sensor(string name, int index, bool defaultHidden, 
      SensorType sensorType, IHardware hardware, 
      ParameterDescription[] parameterDescriptions, ISettings settings) 
    {           
      this.index = index;
      this.defaultHidden = defaultHidden;
      this.sensorType = sensorType;
      this.hardware = hardware;
      Parameter[] parameters = new Parameter[parameterDescriptions == null ?
        0 : parameterDescriptions.Length];
      for (int i = 0; i < parameters.Length; i++ ) 
        parameters[i] = new Parameter(parameterDescriptions[i], this, settings);
      this.parameters = parameters;

      this.settings = settings;
      this.defaultName = name; 
      this.name = settings.Get(
        new Identifier(Identifier, "name").ToString(), name);
    }

    public IHardware Hardware {
      get { return hardware; }
    }

    public SensorType SensorType {
      get { return sensorType; }
    }

    public Identifier Identifier {
      get {
        return new Identifier(hardware.Identifier, 
          sensorType.ToString().ToLower(), index.ToString());
      }
    }

    public string Name {
      get { 
        return name; 
      }
      set {
        if (value != "") 
          name = value;          
        else 
          name = defaultName;
        settings.Set(new Identifier(Identifier, "name").ToString(), name);
      }
    }

    public int Index {
      get { return index; }
    }

    public bool IsDefaultHidden {
      get { return defaultHidden; }
    }

    public IReadOnlyArray<IParameter> Parameters {
      get { return parameters; }
    }

    public float? Value {
      get { 
        return value; 
      }
      set {
        while (values.Count > 0 && 
          (DateTime.Now - values.Peek().Time).TotalMinutes > MAX_MINUTES)
          values.Dequeue();

        if (value.HasValue) {
          sum += value.Value;
          count++;
          if (count == 4) {
            values.Enqueue(new SensorValue(sum / count, DateTime.Now));
            sum = 0;
            count = 0;
          }
        }

        this.value = value;
        if (min > value || !min.HasValue)
          min = value;
        if (max < value || !max.HasValue)
          max = value;
      }
    }

    public float? Min { get { return min; } }
    public float? Max { get { return max; } }

    public void ResetMin() {
      min = null;
    }

    public void ResetMax() {
      max = null;
    }

    public IEnumerable<SensorValue> Values {
      get { return values; }
    }    

    public void Accept(IVisitor visitor) {
      visitor.VisitSensor(this);
    }

    public void Traverse(IVisitor visitor) {
      foreach (IParameter parameter in parameters)
        parameter.Accept(visitor);
    }
  }
}
