/*
Copyright (C) 2019-2020 Maciej Szybiak

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

/*
 * Configuration variable management.
 * Inspired by Q2 cvars.
 */

public delegate void OnCvarSet(ConfVar var);

[Flags]
public enum CvarType
{
    /// <summary> 
    /// Readable/settable variable. 
    /// </summary>
    DEFAULT =   1,
    //TODO: add HIDDEN functionality
    /// <summary> 
    /// Variable hidden from the player and not settable via console/config or whatever.
    /// </summary>
    HIDDEN =    2,
    /// <summary> 
    /// Variable not settable. 
    /// </summary>
    LOCKED =    4,

    /// <summary> </summary>
    STATIC = HIDDEN | LOCKED
}

/// <summary>
/// Configuration variable.
/// </summary>
public class ConfVar
{
    //cvar identifier
    public string Name
    {
        get;
        private set;
    }

    public CvarType Type
    {
        get;
        private set;
    }

    //called when a cvar is set
    public OnCvarSet OnCvarSet;

    private float val;
    public float Value
    {
        get
        {
            return val;
        }
        set
        {
            if (Type.HasFlag(CvarType.LOCKED))
            {
                return;
            }
            val = value;
            text = value.ToString();
            OnCvarSet?.Invoke(this);
        }
    }

    public int Integer
    {
        get
        {
            return (int)Value;
        }
        set
        {
            if (Type.HasFlag(CvarType.LOCKED))
            {
                return;
            }
            Value = value;
        }
    }

    public bool Boolean
    {
        get
        {
            return val != 0;
        }
    }

    private string text;
    public string Text
    {
        get
        {
            return text;
        }
        set
        {
            if (Type.HasFlag(CvarType.LOCKED))
            {
                return;
            }
            if (float.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out float v))
            {
                text = value;
                val = v;
            }
            else
            {
                text = value;
                val = 0;
            }

            OnCvarSet?.Invoke(this);
        }
    }

    /// <summary>
    /// Sets cvar without invoking OnCvarSet
    /// </summary>
    public void NoEventSet(string text)
    {
        if (Type.HasFlag(CvarType.LOCKED))
        {
            return;
        }
        if (float.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out float v))
        {
            val = v;
        }
        else
        {
            val = 0;
        }
        this.text = text;
    }

    public void NoEventSet(float value)
    {
        if (Type.HasFlag(CvarType.LOCKED))
        {
            return;
        }
        val = value;
        text = value.ToString();
    }

    #region constructors

    public ConfVar(string name, string text, OnCvarSet onCvarSet, CvarType type)
    {
        Name = name;
        Text = text;
        OnCvarSet = onCvarSet;
        Type = type;
    }

    public ConfVar(string name, float value, OnCvarSet onCvarSet, CvarType type)
    {
        Name = name;
        val = value;
        OnCvarSet = onCvarSet;
        Type = type;
    }

    public ConfVar(string name, int value, OnCvarSet onCvarSet, CvarType type)
    {
        Name = name;
        Integer = value;
        OnCvarSet = onCvarSet;
        Type = type;
    }

    #endregion
}

public static class Cvar
{
    private static List<ConfVar> cvars = new List<ConfVar>();

    #region array getters

    /// <summary>
    /// Get all cvars of the type provided.
    /// </summary>
    /// <param name="type">Desired type.</param>
    /// <returns>Cvars that contain the type (and possibly other types).</returns>
    public static ConfVar[] GetCvars(CvarType type)
    {
        if(type == CvarType.DEFAULT)
        {
            return cvars.Where(i => i.Type == CvarType.DEFAULT).ToArray();
        }
        return cvars.Where(i => i.Type.HasFlag(type)).ToArray();
    }

    /// <returns>Returns all visible/readable/settable cvars.</returns>
    public static ConfVar[] GetDefaultCvars()
    {
        return GetCvars(CvarType.DEFAULT);
    }

    /// <summary>
    /// For autocompletion.
    /// </summary>
    /// <param name="name">Name to autocomplete.</param>
    /// <returns>List of cvars that have their name start with the string provided.</returns>
    public static ConfVar[] GetCvarCompletions(string name)
    {
        return cvars.Where(i => i.Name.StartsWith(name) && i.Type == CvarType.DEFAULT).ToArray();
    }

    #endregion

    #region adders

    /// <summary>
    /// Adds a new default cvar.
    /// </summary>
    /// <param name="name">Ientifier of the cvar.</param>
    /// <param name="value">Value.</param>
    /// <returns>False if cvar name already exists.</returns>
    public static bool Add(string name, string value)
    {
        return Add(name, value, null, CvarType.DEFAULT);
    }

    /// <summary>
    /// Adds a new default cvar.
    /// </summary>
    /// <param name="name">Ientifier of the cvar.</param>
    /// <param name="value">Value.</param>
    /// <param name="onCvarSet">Function called on cvar set (null if none).</param>
    /// <param name="type">Cvar type.</param>
    /// <returns>False if cvar name already exists.</returns>
    public static bool Add(string name, string value, OnCvarSet onCvarSet, CvarType type)
    {
        if (cvars.Any(i => i.Name == name))
        {
            return false;
        }

        cvars.Add(new ConfVar(name, value, onCvarSet, type));
        return true;
    }

    #endregion

    #region getters

    /// <summary>
    /// Finds or creates a new default cvar.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns>Cvar with the provided name and value.</returns>
    public static ConfVar Get(string name, string value)
    {
        return Get(name, value, null, CvarType.DEFAULT);
    }

    /// <summary>
    /// Finds or creates a new default cvar.
    /// </summary>
    /// <param name="name">Ientifier of the cvar.</param>
    /// <param name="value">Value.</param>
    /// <param name="onCvarSet">Function called on cvar set (null if none).</param>
    /// <param name="type">Cvar type.</param>
    /// <returns>Cvar with the provided parameter values.</returns>
    public static ConfVar Get(string name, string value, OnCvarSet onCvarSet, CvarType type)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar == null)
        {
            cvar = new ConfVar(name, value, onCvarSet, type);
            cvars.Add(cvar);
        }
        else
        {
            cvar.Text = value;
        }

        return cvar;
    }

    /// <summary>
    /// Gets a cvar without creating a new one.
    /// </summary>
    /// <param name="name">Identifier.</param>
    /// <param name="cvar">Output cvar.</param>
    /// <returns>True if the cvar exists.</returns>
    public static bool WeakGet(string name, out ConfVar cvar)
    {
        cvar = cvars.FirstOrDefault(i => i.Name == name);

        return cvar != null;
    }

    #endregion

    #region setters
    
    /// <summary>
    /// Sets cvar by string value.
    /// </summary>
    /// <returns>True if cvar was successfully set.</returns>
    public static bool Set(string name, string text)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if(cvar != null && !cvar.Type.HasFlag(CvarType.LOCKED))
        {
            cvar.Text = text;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets cvar by float value.
    /// </summary>
    /// <returns>True if cvar was successfully set.</returns>
    public static bool Set(string name, float value)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar != null && !cvar.Type.HasFlag(CvarType.LOCKED))
        {
            cvar.Value = value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets cvar by integer value.
    /// </summary>
    /// <returns>True if cvar was successfully set.</returns>
    public static bool Set(string name, int value)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar != null && !cvar.Type.HasFlag(CvarType.LOCKED))
        {
            cvar.Integer = value;
            return true;
        }

        return false;
    }

    #endregion

    #region value getters

    public static string Text(string name)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar != null)
        {
            return cvar.Text;
        }
        return "";
    }

    public static float Value(string name)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar != null)
        {
            return cvar.Value;
        }
        return 0;
    }

    public static int Integer(string name)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar != null)
        {
            return cvar.Integer;
        }
        return 0;
    }

    public static bool Boolean(string name)
    {
        ConfVar cvar = cvars.FirstOrDefault(i => i.Name == name);

        if (cvar != null)
        {
            return cvar.Integer != 0;
        }
        return false;
    }

    #endregion

    public static bool Exists(string name)
    {
        return cvars.Any(i => i.Name == name);
    }
}
