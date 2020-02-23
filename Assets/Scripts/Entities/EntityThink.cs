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

using UnityEngine;

/*
 * Stores entity think methods.
 */

public delegate void EntityThinkMethod(GameModel model);

public static class EntityThink
{
    public static void FuncRotatingThink(GameModel model)
    {
        //float spd = model.Ent.Speed == 0 ? 200 : model.Ent.Speed;
        //model.transform.Rotate(model.Ent.movedir, spd * Time.deltaTime);
        //model.Ent.MoveAngles = model.transform.eulerAngles;
    }

    public static void TriggerFinishThink(GameModel model)
    {
        
    }

    public static void GenericWeaponThink(GameModel model)
    {
        if (model.entModel)
        {
            model.entModel.transform.Rotate(0, 100 * Time.deltaTime, 0);
        }
    }
}
