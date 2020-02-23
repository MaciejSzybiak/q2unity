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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Manages the existence of a projectile (movement, impact, explosion etc.).
 */

public class Projectile : MonoBehaviour
{
    public List<ParticleSystem> trailParticles;
    public ParticleSystem explosionParticles;
    public AudioSource explosionSound;
    public AudioSource flySound;

    public float speed;
    public float damage;
    public float radiusDamage;
    public float damageRadius;

    private float aliveTime = 0;

    private float scale;

    private bool exploding = false;

    private void Start()
    {
        scale = Globals.scale.Value;
    }

    private void Update()
    {
        if (exploding)
        {
            return;
        }

        aliveTime += Time.unscaledDeltaTime;

        //stop exisiting at 8 secs...
        //TODO: add an explosion?
        if(aliveTime > 8)
        {
            Explode();
            return;
        }

        Vector3 start = transform.position / scale;
        Vector3 end = start + transform.forward * speed * Time.unscaledDeltaTime;

        TraceT trace = Trace.CL_Trace(start, Vector3.zero, Vector3.zero, end);

        if (trace.allsolid)
        {
            Hit(trace);
            return; //shouldnt happen, add destroy here?
        }

        //move up to trace end
        transform.position = trace.endpos * scale;

        if(trace.fraction < 1)
        {
            Hit(trace);
        }
    }

    //only radius damage is needed
    private void Hit(TraceT trace)
    {
        //Console.DebugLog("Projectile hit at " + trace.endpos);

        Vector3 projectilePos = transform.position / scale;
        Vector3 playerPos = PlayerState.currentOrigin;

        if((projectilePos - playerPos).magnitude > damageRadius)
        {
            Explode();
            return;
        }

        Vector3 v = PlayerState.mins + PlayerState.maxs;
        v = playerPos + v * 0.5f;

        v = projectilePos - v;
        //Console.DebugLog("V is " + v.magnitude);

#if DEBUG
        float dmg = radiusDamage;
        if (Cvar.Exists("debug_rocketmult"))
        {
            dmg *= Cvar.Value("debug_rocketmult");
        }
        float knockback = dmg - 0.5f * v.magnitude;
#else
        float knockback = radiusDamage - 0.5f * v.magnitude;
#endif
        knockback *= 0.5f;

        if (knockback > 0)
        {
            Vector3 knockDir = playerPos - projectilePos;
            float mass = 200;

            Vector3 velocity = knockDir.normalized * (1600 * knockback / mass);
            //Console.DebugLog("Knockback force " + velocity.magnitude);

            PlayerState.addVelocities += velocity;
        }

        Explode();
    }

    //will destroy the projectile gameObject
    private void Explode()
    {
        exploding = true;

#pragma warning disable CS0618 // Type or member is obsolete
        foreach(ParticleSystem p in trailParticles)
        {
            p.enableEmission = false; //hold particles. Unfixable deprecation: ParticleSystem.emission is readonly
        }
#pragma warning restore CS0618 // Type or member is obsolete

        //stop rendering rocket model
        GetComponent<MeshRenderer>().enabled = false;

        if (explosionParticles)
        {
            explosionParticles.Play();
        }

        //sounds
        flySound.Stop();
        explosionSound.Play();

        StartCoroutine(ExplodeCoroutine());
    }

    private IEnumerator ExplodeCoroutine()
    {
        if (explosionParticles && explosionParticles.isPlaying)
        {
            yield return new WaitForSecondsRealtime(4f);
        }

        Destroy(gameObject);
    }
}
