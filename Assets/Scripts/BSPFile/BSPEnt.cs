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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * BSPEnt is responsible for parsing and storing entity's spawn data (used later for
 * creating an entity in the world).
 */

//Q2 entities contain four types of properties
public enum SpawnFieldType
{
    tString,
    tFloat,
    tInt,
    tVector
}

//entity's properties are stored in spawn fields
public struct SpawnField
{
    public readonly string name;    //property name
    public readonly int index;      //property index
    public readonly SpawnFieldType fieldType; //which array do we look into

    public SpawnField(string name, int index, SpawnFieldType fieldType)
    {
        this.name = name;
        this.index = index;
        this.fieldType = fieldType;
    }
}

public class BSPEnt
{
    //there are smarter ways to do this (generics?)
    //this is also hard to maintain...
    public static readonly SpawnField[] spawnFields =
    {
        new SpawnField("classname", 0, SpawnFieldType.tString),
        new SpawnField("model", 1, SpawnFieldType.tString),
        new SpawnField("target", 2, SpawnFieldType.tString),
        new SpawnField("targetname", 3, SpawnFieldType.tString),
        new SpawnField("pathtarget", 4, SpawnFieldType.tString),
        new SpawnField("deathtarget", 5, SpawnFieldType.tString),
        new SpawnField("killtarget", 6, SpawnFieldType.tString),
        new SpawnField("combattarget", 7, SpawnFieldType.tString),
        new SpawnField("message", 8, SpawnFieldType.tString),
        new SpawnField("team", 9, SpawnFieldType.tString),
        new SpawnField("map", 10, SpawnFieldType.tString),

        new SpawnField("spawnflags", 0, SpawnFieldType.tInt),
        new SpawnField("style", 1, SpawnFieldType.tInt),
        new SpawnField("count", 2, SpawnFieldType.tInt),
        new SpawnField("health", 3, SpawnFieldType.tInt),
        new SpawnField("sounds", 4, SpawnFieldType.tInt),
        new SpawnField("dmg", 5, SpawnFieldType.tInt),
        new SpawnField("mass", 6, SpawnFieldType.tInt),

        new SpawnField("speed", 0, SpawnFieldType.tFloat),
        new SpawnField("accel", 1, SpawnFieldType.tFloat),
        new SpawnField("decel", 2, SpawnFieldType.tFloat),
        new SpawnField("wait", 3, SpawnFieldType.tFloat),
        new SpawnField("delay", 4, SpawnFieldType.tFloat),
        new SpawnField("random", 5, SpawnFieldType.tFloat),
        new SpawnField("volume", 6, SpawnFieldType.tFloat),
        new SpawnField("attenuation", 7, SpawnFieldType.tFloat),
        new SpawnField("angle", 8, SpawnFieldType.tFloat),

        new SpawnField("move_origin", 0, SpawnFieldType.tVector),
        new SpawnField("move_angles", 1, SpawnFieldType.tVector),
        new SpawnField("origin", 2, SpawnFieldType.tVector),
        new SpawnField("angles", 3, SpawnFieldType.tVector),

        //q2 calls these temporary
        new SpawnField("noise", 11, SpawnFieldType.tString),
        new SpawnField("item", 12, SpawnFieldType.tString),
        new SpawnField("sky", 13, SpawnFieldType.tString),
        new SpawnField("nextmap", 14, SpawnFieldType.tString),

        new SpawnField("lip", 7, SpawnFieldType.tInt),
        new SpawnField("distance", 8, SpawnFieldType.tInt),
        new SpawnField("height", 9, SpawnFieldType.tInt),
        new SpawnField("gravity", 10, SpawnFieldType.tInt),

        new SpawnField("pausetime", 9, SpawnFieldType.tFloat),
        new SpawnField("skyrotate", 10, SpawnFieldType.tFloat),
        new SpawnField("minyaw", 11, SpawnFieldType.tFloat),
        new SpawnField("maxyaw", 12, SpawnFieldType.tFloat),
        new SpawnField("minpitch", 13, SpawnFieldType.tFloat),
        new SpawnField("maxpitch", 14, SpawnFieldType.tFloat),

        new SpawnField("skyaxis", 4, SpawnFieldType.tVector)
    };

    public Dictionary<string, string> strings = new Dictionary<string, string>();

    //parseable data

    #region strings

    public string Classname
    {
        get
        {
            return stringProperties[0];
        }
    }

    public string Model
    {
        get
        {
            return stringProperties[1];
        }
    }

    public string Target
    {
        get
        {
            return stringProperties[2];
        }
    }

    public string Targetname
    {
        get
        {
            return stringProperties[3];
        }
    }

    public string Pathtarget
    {
        get
        {
            return stringProperties[4];
        }
    }

    public string Deathtarget
    {
        get
        {
            return stringProperties[5];
        }
    }

    public string Killtarget
    {
        get
        {
            return stringProperties[6];
        }
    }

    public string Combattarget
    {
        get
        {
            return stringProperties[7];
        }
    }

    public string Message
    {
        get
        {
            return stringProperties[8];
        }
    }

    public string Team
    {
        get
        {
            return stringProperties[9];
        }
    }

    public string Map
    {
        get
        {
            return stringProperties[10];
        }
    }

    public string Noise
    {
        get
        {
            return stringProperties[11];
        }
    }

    public string Item
    {
        get
        {
            return stringProperties[12];
        }
    }

    public string Sky
    {
        get
        {
            return stringProperties[13];
        }
    }

    public string Nextmap
    {
        get
        {
            return stringProperties[14];
        }
    }

    #endregion

    #region integers

    public int Spawnflags
    {
        get
        {
            return intProperties[0];
        }
    }

    public int Style
    {
        get
        {
            return intProperties[1];
        }
    }

    public int Count
    {
        get
        {
            return intProperties[2];
        }
    }

    public int Health
    {
        get
        {
            return intProperties[3];
        }
    }

    public int Sounds
    {
        get
        {
            return intProperties[4];
        }
    }

    public int Dmg
    {
        get
        {
            return intProperties[5];
        }
    }

    public int Mass
    {
        get
        {
            return intProperties[6];
        }
    }

    public int Lip
    {
        get
        {
            return intProperties[7];
        }
    }

    public int Distance
    {
        get
        {
            return intProperties[8];
        }
    }

    public int Height
    {
        get
        {
            return intProperties[9];
        }
    }

    public int Gravity
    {
        get
        {
            return intProperties[10];
        }
    }

    #endregion

    #region floats

    public float Speed
    {
        get
        {
            return floatProperties[0];
        }
    }

    public float Accel
    {
        get
        {
            return floatProperties[1];
        }
    }

    public float Decel
    {
        get
        {
            return floatProperties[2];
        }
    }

    public float Wait
    {
        get
        {
            return floatProperties[3];
        }
    }

    public float Delay
    {
        get
        {
            return floatProperties[4];
        }
    }

    public float Random
    {
        get
        {
            return floatProperties[5];
        }
    }

    public float Volume
    {
        get
        {
            return floatProperties[6];
        }
    }

    public float Attenuation
    {
        get
        {
            return floatProperties[7];
        }
    }

    public float Angle
    {
        get
        {
            return floatProperties[8];
        }
    }

    public float Pausetime
    {
        get
        {
            return floatProperties[9];
        }
    }

    public float Skyrotate
    {
        get
        {
            return floatProperties[10];
        }
    }

    public float Minyaw
    {
        get
        {
            return floatProperties[11];
        }
    }

    public float Maxyaw
    {
        get
        {
            return floatProperties[12];
        }
    }

    public float Minpitch
    {
        get
        {
            return floatProperties[13];
        }
    }

    public float Maxpitch
    {
        get
        {
            return floatProperties[14];
        }
    }

    #endregion

    #region vectors

    public Vector3 MoveOrigin
    {
        get
        {
            return vecProperties[0];
        }
        set
        {
            vecProperties[0] = value;
        }
    }

    public Vector3 MoveAngles
    {
        get
        {
            return vecProperties[1];
        }
        set
        {
            vecProperties[1] = value;
        }
    }

    public Vector3 Origin
    {
        get
        {
            return vecProperties[2];
        }
    }

    public Vector3 Angles
    {
        get
        {
            return vecProperties[3] != Vector3.zero ? (Vector3.one * 90) - vecProperties[3] : new Vector3(0, 90 - Angle, 0);
        }
    }

    public Vector3 Skyaxis
    {
        get
        {
            return vecProperties[4];
        }
    }

    #endregion

    #region local properties

    public Vector3 movedir;
    public GameModel[] targetEntities;

    #endregion

    private readonly string[] stringProperties = new string[15];
    private readonly int[] intProperties = new int[11];
    private readonly float[] floatProperties = new float[15];
    private readonly Vector3[] vecProperties = new Vector3[5];

    public EntityThinkMethod Think
    {
        get;
        private set;
    }

    public EntityTouchMethod Touch
    {
        get;
        private set;
    }

    public void SetThinkAndTouch(EntityThinkMethod think, EntityTouchMethod touch)
    {
        Think = think;
        Touch = touch;
    }

    public void ParseEntStrings()
    {
        SpawnField spawnField;
        float scale = Globals.scale.Value;

        foreach(var str in strings)
        {
            spawnField = spawnFields.FirstOrDefault(i => i.name == str.Key);
            if (spawnField.name == str.Key)
            {
                //found it
                switch (spawnField.fieldType)
                {
                    case SpawnFieldType.tString:
                        stringProperties[spawnField.index] = str.Value; //string don't need to be parsed :)
                        break;
                    case SpawnFieldType.tInt:
                        if(int.TryParse(str.Value, out int i))
                        {
                            intProperties[spawnField.index] = i;
                            break;
                        }
                        goto default; //fail
                    case SpawnFieldType.tFloat:
                        if (float.TryParse(str.Value, out float f))
                        {
                            floatProperties[spawnField.index] = f;
                            break;
                        }
                        goto default; //fail
                    case SpawnFieldType.tVector:
                        if(Utils.ParseThreeNumbers(str.Value, out Vector3 v, true))
                        {
                            vecProperties[spawnField.index] = v * scale;
                            break;
                        }
                        goto default; //fail
                    default:
                        Console.LogWarning("Couldn't parse entity property \"" + str.Key + "\" value: " + str.Value);
                        break;  
                }
            }
            else
            {
                Console.LogWarning("Entity property \"" + str.Key + "\" not found!");
            }
        }
    }
}
