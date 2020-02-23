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
using UnityEngine;

/*
 * Calls for player position updates and manages async physics.
 */

[Flags]
public enum PMFlags
{
    PMF_DUCKED = 1,
    PMF_JUMP_HELD = 2,
    PMF_ON_GROUND = 4,
    PMF_TIME_WATERJUMP = 8
}

public class Pmove : MonoBehaviour
{
    public GameObject storeObjectPrefab;
    private GameObject storeObjectInstance;

    //camera
    public Camera Camera;
    public Camera maskCamera;
    private float camX = 0;
    private float camY = 0;

    private ConfVar mouseSensitivity;

    //smoot camera
    private ConfVar smoothCamera;
    float desiredCamX;
    float desiredCamY;

    //collider
    public BoxCollider colliderObject;
    public AudioSource jumpSoundSource;

    //move state
    private float viewheight;

    private PMFlags pmflags;

    private Vector3 out_origin = Vector3.zero;
    private Vector3 out_velocity = Vector3.zero;
    private Vector3 origin = Vector3.zero;
    private Vector3 velocity = Vector3.zero;

    private Vector3 previous_origin = Vector3.zero;
    private Vector3 previous_camera_position = Vector3.zero;
    private bool snapRequired = false;

    //async
    private PMFlags asyncFlags;

    private Vector3 lastAsyncOrigin;
    private Vector3 lastAsyncVelocity;
    private Vector3 lastAsyncAddVelocity = Vector3.zero;
    private float elapsedAsyncFrametime;
    private bool asyncSnapRequired = false;

    //store
    private Vector3 storedOrigin;
    private float storedCamY;
    private bool hasStore = false;

    private void MouseLook()
    {
        //FIXME: use globals
        if(Cvar.Boolean("console_open") || Globals.menuopen.Boolean)
        {
            return;
        }

        if (smoothCamera.Boolean)
        {
            desiredCamX += Input.GetAxisRaw("Mouse Y") * mouseSensitivity.Value;
            desiredCamY += Input.GetAxisRaw("Mouse X") * mouseSensitivity.Value;

            camX = Mathf.Lerp(camX, desiredCamX, smoothCamera.Value);
            camY = Mathf.Lerp(camY, desiredCamY, smoothCamera.Value);
        }
        else
        {
            camX += Input.GetAxisRaw("Mouse Y") * mouseSensitivity.Value;
            camY += Input.GetAxisRaw("Mouse X") * mouseSensitivity.Value;

        }

        camX = Mathf.Clamp(camX, -90, 90);

        Camera.transform.rotation = Quaternion.Euler(-camX, camY, 0);
    }

    private void Awake()
    {
        //register commands
        CommandManager.RegisterCommand("recall", Recall_c, "resets player position", CommandType.normal);
        CommandManager.RegisterCommand("store", Store_c, "stores player position", CommandType.normal);
        CommandManager.RegisterCommand("remstore", RemStore_, "removes stored player position", CommandType.normal);
        CommandManager.RegisterCommand("setfps", SetFps_c, "sets framerate cap", CommandType.normal);
        CommandManager.RegisterCommand("gamemode", PlayerState.GameMode_c, "sets game mode (fly, spectating, training, timeattack or 0, 1, 2, 3)", CommandType.normal);
        CommandManager.RegisterCommand("noclip", PlayerState.Noclip_c, "toggles noclip cvar", CommandType.normal);

        //add cvars
        Cvar.Add("noclip", "0", PlayerState.OnCvarNoclipSet, CvarType.DEFAULT);
        smoothCamera = Cvar.Get("smooth_camera", "0");
        mouseSensitivity = Cvar.Get("mouse_sensitivity", "1.35");
        Globals.flySpeed = Cvar.Get("flyspeed", "300");

        //default movement binds
        BindManager.SetBind(KeyCode.A, "+left", BindType.hold, false);
        BindManager.SetBind(KeyCode.S, "+back", BindType.hold, false);
        BindManager.SetBind(KeyCode.D, "+right", BindType.hold, false);
        BindManager.SetBind(KeyCode.W, "+forward", BindType.hold, false);
        BindManager.SetBind(KeyCode.Mouse1, "+jump", BindType.hold, false);
        BindManager.SetBind(KeyCode.LeftControl, "+duck", BindType.hold, false);

        //store and recall
        BindManager.SetBind(KeyCode.R, "recall", BindType.toggle, false);
        BindManager.SetBind(KeyCode.Q, "store", BindType.toggle, false);
        BindManager.SetBind(KeyCode.P, "remstore", BindType.toggle, false);

        //state binds
        BindManager.SetBind(KeyCode.Alpha1, "gamemode flying", BindType.toggle, false);
        BindManager.SetBind(KeyCode.Alpha2, "gamemode training", BindType.toggle, false);
        BindManager.SetBind(KeyCode.Alpha3, "gamemode timeattack", BindType.toggle, false);
        BindManager.SetBind(KeyCode.N, "noclip", BindType.toggle, false);

        //default fps binds
        BindManager.SetBind(KeyCode.Z, "setfps 66", BindType.toggle, false);
        BindManager.SetBind(KeyCode.X, "setfps 22", BindType.toggle, false);
        BindManager.SetBind(KeyCode.C, "setfps 120", BindType.toggle, false);
        BindManager.SetBind(KeyCode.V, "setfps 27", BindType.toggle, false);
        BindManager.SetBind(KeyCode.B, "setfps 90", BindType.toggle, false);
        BindManager.SetBind(KeyCode.G, "setfps 200", BindType.toggle, false);
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;

        PlayerState.teleportEvent.AddListener(OnTeleportEvent);
    }

    private void FixedUpdate()
    {
        //check if a map is loaded and async is on
        if (Cvar.Integer("maploaded") == 0 || !Globals.async.Boolean)
        {
            return;
        }

        PlayerMoveData pmd = new PlayerMoveData
        {
            inOrigin = lastAsyncOrigin,
            inVelocity = lastAsyncVelocity,

            inForward = Camera.transform.forward,
            inRight = Camera.transform.right,
            camX = camX,
            camY = camY,
            addVelocities = lastAsyncAddVelocity,
            gravity = PlayerState.pm_gravity,
            frametime = Time.fixedDeltaTime,
            initialSnap = asyncSnapRequired,
            flags = asyncFlags
        };

        //run async frame
        PlayerMove.Pmove(ref pmd);

        //snap doesn't allow going back
        if (asyncSnapRequired)
        {
            previous_origin = pmd.outOrigin;
            asyncSnapRequired = false;
        }

        lastAsyncOrigin = pmd.outOrigin;
        lastAsyncVelocity = pmd.outVelocity;
        lastAsyncAddVelocity = Vector3.zero;
        asyncFlags = pmd.flags;

        //start counting simulated time again
        elapsedAsyncFrametime = 0;

        //overwrite all sync variables with async frame. If everything is done correctly the values should be matching
        origin = pmd.outOrigin * 0.125f;
        out_origin = pmd.outOrigin;
        out_velocity = pmd.outVelocity;

        velocity = pmd.outVelocity * 0.125f;
        pmflags = pmd.flags;
        viewheight = pmd.viewheight;

        //TODO: is everything below this even needed?
        ApplyPmoveToCamera();

        PlayerState.currentSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        PlayerState.currentVieweight = viewheight;
        PlayerState.currentOrigin = origin;

        PlayerState.mins = pmd.mins;
        PlayerState.maxs = pmd.maxs;
    }

    private void Update()
    {
        //check if a map is loaded
        if (Cvar.Integer("maploaded") == 0)
        {
            return;
        }

        //mouse movement
        MouseLook();

        PlayerMoveData pmd;
        if (Globals.async.Boolean)
        {
            elapsedAsyncFrametime += Time.deltaTime;

            pmd = new PlayerMoveData
            {
                inOrigin = lastAsyncOrigin,
                inVelocity = lastAsyncVelocity,

                inForward = Camera.transform.forward,
                inRight = Camera.transform.right,

                camX = camX,
                camY = camY,

                addVelocities = PlayerState.addVelocities,
                gravity = PlayerState.pm_gravity,
                frametime = elapsedAsyncFrametime, //run delta time from last async frame
                initialSnap = snapRequired,
                flags = pmflags
            };
        }
        else
        {
            pmd = new PlayerMoveData
            {
                inOrigin = out_origin,
                inVelocity = out_velocity,

                inForward = Camera.transform.forward,
                inRight = Camera.transform.right,
                camX = camX,
                camY = camY,
                addVelocities = PlayerState.addVelocities,
                gravity = PlayerState.pm_gravity,
                frametime = Time.deltaTime, //run current delta time
                initialSnap = snapRequired,
                flags = pmflags
            };
        }

        //queue velocities for async
        lastAsyncAddVelocity += PlayerState.addVelocities;
        PlayerState.addVelocities = Vector3.zero;

        //get pmove
        PlayerMove.Pmove(ref pmd);

        //play jump sound
        if(pmd.jumped)
        {
            jumpSoundSource.Play();
        }

        //snapped -> position changed by entity. Don't allow going back.
        if (snapRequired)
        {
            previous_origin = pmd.outOrigin;
            snapRequired = false;
        }

        //set data
        origin = pmd.outOrigin * 0.125f;
        velocity = pmd.outVelocity * 0.125f;

        pmflags = pmd.flags;
        viewheight = pmd.viewheight;

        //update camera
        ApplyPmoveToCamera();

        //update player state info
        PlayerState.currentSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        PlayerState.currentVieweight = viewheight;
        PlayerState.currentOrigin = origin;

        PlayerState.mins = pmd.mins;
        PlayerState.maxs = pmd.maxs;

        //snap position and store old origin
        out_origin = origin * 8;
        out_velocity = pmd.outVelocity;

        //FIXME: do this only on async switch
        if (!Globals.async.Boolean)
        {
            lastAsyncAddVelocity = Vector3.zero;
            lastAsyncOrigin = out_origin;
            lastAsyncVelocity = out_velocity;
            asyncFlags = pmflags;
        }
    }

    #region command handlers

    private void Store_c(string[] args)
    {
        storedOrigin = out_origin * 0.125f;
        storedCamY = camY;
        hasStore = true;

        if (storeObjectPrefab)
        {
            if (!storeObjectInstance)
            {
                storeObjectInstance = Instantiate(storeObjectPrefab);
            }
            storeObjectInstance.transform.position = (storedOrigin - Vector3.up * viewheight / 3) * Globals.scale.Value;
        }
    }

    private void RemStore_(string[] args)
    {
        hasStore = false;
        Destroy(storeObjectInstance);
    }

    private void Recall_c(string[] args)
    {
        //reset run
        RunTimer.Instance.ResetRun();

        //reset player items
        PlayerState.ResetPlayerItems();

        Vector3 spawnPosition = PlayerState.spawnOrigin;
        Vector3 spawnAngle = PlayerState.spawnAngles;

        //check store
        if (hasStore && (PlayerState.mode == GameMode.training || PlayerState.mode == GameMode.flying))
        {
            //recall angles
            Camera.transform.rotation = Quaternion.Euler(0, storedCamY, 0);
            camX = 0;
            camY = storedCamY;

            //recall origin
            origin = storedOrigin;
            origin += Vector3.up * 0.125f;
        }
        else
        {
            //set camera angles
            Camera.transform.rotation = Quaternion.Euler(spawnAngle);
            camX = -spawnAngle.x;
            camY = spawnAngle.y;
            //no Z angle :>

            origin = spawnPosition / Globals.scale.Value;
            origin += Vector3.up * 0.125f; //move up a little bit
        }
        previous_origin = origin;
        previous_camera_position = origin;
        velocity = out_velocity = Vector3.zero;
        out_origin = origin * 8;

        lastAsyncOrigin = out_origin;
        lastAsyncVelocity = out_velocity;

        //request snap
        snapRequired = true;
        asyncSnapRequired = true;
    }

    private void SetFps_c(string[] args)
    {
        if(args.Length != 1) //no fps specified
        {
            return;
        }
        if(int.TryParse(args[0], out int val))
        {
            Cvar.Set("maxfps", val);
        }
    }

    #endregion

    #region events

    private void OnTeleportEvent()
    {
        Vector3 pos = PlayerState.teleportDestination;
        Vector3 rot = PlayerState.teleportAngles;

        //set angles
        Camera.transform.rotation = Quaternion.Euler(0, rot.y, 0);
        camX = 0;
        camY = rot.y;

        //set origin
        origin = pos;
        origin += Vector3.up * 1.125f;
        out_origin = origin * 8;

        //kill velocity
        velocity = Vector3.zero;
        out_velocity = Vector3.zero;

        //set async
        lastAsyncOrigin = out_origin;
        lastAsyncVelocity = out_velocity;

        //request snap
        snapRequired = true;
        asyncSnapRequired = true;
    }

    #endregion

    bool isLerping = false;
    private void ApplyPmoveToCamera()
    {
        float scale = Globals.scale.Value;
        Vector3 newCamPos = new Vector3(origin.x, origin.y, origin.z) + Vector3.up * viewheight;

        //smooth out stepping up and crouching
        float step = newCamPos.y - previous_camera_position.y;

        if (!pmflags.HasFlag(PMFlags.PMF_ON_GROUND) || step == 0)
        {
            isLerping = false;
        }
        else
        {
            isLerping = true;
        }

        if (isLerping)
        {
            newCamPos.y = Mathf.Lerp(previous_camera_position.y, newCamPos.y, (pmflags.HasFlag(PMFlags.PMF_DUCKED) ? 25f : 12.5f) * Time.deltaTime);
        }

        previous_camera_position = newCamPos;
        Camera.transform.position = new Vector3(newCamPos.x, newCamPos.y, newCamPos.z) * scale;

        //set collider
        colliderObject.gameObject.transform.position = Camera.transform.position - Vector3.up * viewheight * scale;

        //please unity, just let me set collider bounds...
        Bounds b = new Bounds();
        b.SetMinMax(PlayerState.mins * scale, PlayerState.maxs * scale);

        colliderObject.center = b.center;
        colliderObject.size = b.extents * 2;
    }
}
