using Harmony;
using Planetbase;
using UnityEngine;

namespace Planetbase_FreeBuilding.Patches.Planetbase.Module
{
    [HarmonyPatch(typeof(global::Planetbase.Module))]
    [HarmonyPatch("canPlaceModule")]
    public class CanPlaceModuleClass
    {
        public static bool Prefix(global::Planetbase.Module __instance, ref bool __result, Vector3 position, Vector3 normal, float size)
        {
            __result = ReplacementMethod(__instance, position, normal, size);
            return false;
        }

        public static bool ReplacementMethod(global::Planetbase.Module instance, Vector3 position, Vector3 normal, float size)
        {
            var floorHeight = Singleton<TerrainGenerator>.getInstance().getFloorHeight();
            var heightDiff = position.y - floorHeight;

            var isMine = instance.hasFlag(ModuleType.FlagMine);
            if (isMine)
            {
                if (heightDiff < 1f || heightDiff > 3f)
                {
                    // mine must be a little elevated
                    return false;
                }
            }
            else if (heightDiff > 0.1f || heightDiff < -0.1f)
            {
                // not at floor level
                return false;
            }

            // here we're approximating the circumference of the structure with 8 points
            // and will check that all these points are level with the floor
            var reducedRadius = size * 0.75f;
            var angledReducedRadius = reducedRadius * 1.41421354f * 0.5f;
            var circumference = new[]
            {
                position + new Vector3(reducedRadius, 0f, 0f),
                position + new Vector3(-reducedRadius, 0f, 0f),
                position + new Vector3(0f, 0f, reducedRadius),
                position + new Vector3(0f, 0f, -reducedRadius),
                position + new Vector3(angledReducedRadius, 0f, angledReducedRadius),
                position + new Vector3(angledReducedRadius, 0f, -angledReducedRadius),
                position + new Vector3(-angledReducedRadius, 0f, angledReducedRadius),
                position + new Vector3(-angledReducedRadius, 0f, -angledReducedRadius)
            };

            if (isMine)
            {
                // above we verified that it is a bit elevated
                // now make sure that at least one point is near level ground
                var isValid = false;
                for (var i = 0; i < circumference.Length; i++)
                {
                    PhysicsUtil.findFloor(circumference[i], out var floor);
                    if (floor.y < floorHeight + 1f || floor.y > floorHeight - 1f)
                    {
                        isValid = true;
                        break;
                    }
                }

                if (!isValid)
                {
                    return false;
                }
            }
            else
            {
                // Make sure all points are near level ground
                for (var j = 0; j < circumference.Length; j++)
                {
                    PhysicsUtil.findFloor(circumference[j], out var floor);
                    if (floor.y > floorHeight + 2f || floor.y < floorHeight - 1f)
                    {
                        return false;
                    }
                }
            }

            // Can only be 375 units away from center of map
            var mapCenter = new Vector2(TerrainGenerator.TotalSize, TerrainGenerator.TotalSize) * 0.5f;
            var distToCenter = (mapCenter - new Vector2(position.x, position.z)).magnitude;
            if (distToCenter > 375f)
            {
                return false;
            }

            // anyPotentialLinks limits connection to 20 (on top of some other less relevant checks)
            if (Construction.mConstructions.Count > 1 && !instance.anyPotentialLinks(position))
            {
                return false;
            }

            var array2 = Physics.SphereCastAll(position + Vector3.up * 20f, size * 0.5f + 3f, Vector3.down, 40f, 4198400);
            if (array2 != null)
            {
                for (var k = 0; k < array2.Length; k++)
                {
                    var raycastHit = array2[k];
                    var gameObject = raycastHit.collider.gameObject.transform.root.gameObject;
                    var construction = Construction.mConstructionDictionary[gameObject];
                    if (construction != null)
                    {
                        //if (construction is Connection)
                        //{
                        //    return false;
                        //}
                        var distToConstruction = (position - construction.getPosition()).magnitude - instance.getRadius() - construction.getRadius();
                        if (distToConstruction < 3f)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Not hitting construction: " + gameObject.name);
                    }
                }
            }

            // Check that it's away from the ship
            if (Physics.CheckSphere(position, size * 0.5f + 3f, 65536))
            {
                return false;
            }

            return true;
        }
    }
}