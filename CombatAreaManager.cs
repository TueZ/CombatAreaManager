using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatAreaManager {
    // VIGTIGT! I forhold til tiles, så er x aksen kun hvert andet tal. Dette kaldes Double Width. 

    //private List<Vector2Int[]> totalMap = new List<Vector2Int[]>();
    private Vector2Int[] combinedMap = new Vector2Int[0];
    private Vector3 realWorldTransform;
    //private const float xTransformFactor = 1.1f;
    //private const float yTransformFactor = 0.6f;
    private Dictionary<Vector2Int, UnitBase> gridUnitRelation = new Dictionary<Vector2Int, UnitBase>();
    private Dictionary<Vector2Int, TerrainObstacle> gridObstacleRelation = new Dictionary<Vector2Int, TerrainObstacle>();
    private List<GridTile> gridTiles = new List<GridTile>();
    private GridTile centerGrid = null; 
    private Dictionary<Vector2Int, Vector3> gridRealWorldRelation = new Dictionary<Vector2Int, Vector3>();
    private CombatManager combatManager; 

    public CombatAreaManager(GridTile centerGrid, CombatManager combatManager) {
        this.combatManager = combatManager; 
        this.centerGrid = centerGrid; 
    }

    /// <summary>
    /// Add a combat area to the existing combat map. 
    /// </summary>
    /// <param name="toAdd"></param>
    public Vector2Int[] AddAreaToMap(GridTile gridTile) {
        gridTiles.Add(gridTile);        

        var relation = gridTile.GetGridRealWorldRelation(centerGrid);
        foreach (var key in relation.Keys) {
            if (!gridRealWorldRelation.ContainsKey(key)) {
                gridRealWorldRelation.Add(key, relation[key]);
            }
        }

        var obstacles = gridTile.ScanForTerrainObstacles(relation);
        foreach (var key in obstacles.Keys) {
            if (!gridObstacleRelation.ContainsKey(key)) {
                gridObstacleRelation.Add(key, obstacles[key]);
                obstacles[key].combatAreaManager = this; 
            }
        }

        //var area = relation.Keys.ToArray();
        var area = relation.Keys.Where(t => !combinedMap.Any(c => c.x == t.x && c.y == t.y)).ToArray();
        ConcatArray(ref combinedMap, area);

        return area; 
    }

    /// <summary>
    /// Returns the current combat map. 
    /// </summary>
    /// <returns></returns>
    public Vector2Int[] GetCombatMap() {
        return combinedMap;
    }

    /// <summary>
    /// Returns all adjacent tiles. 
    /// </summary>
    /// <param name="from"></param>
    /// <returns></returns>
    public Vector2Int[] GetAdjacentTiles(Vector2Int from) {
        List<Vector2Int> toReturn = new List<Vector2Int>();
        foreach (var tile in combinedMap) {
            if (GetDistance(from, tile) == 1) {                
                toReturn.Add(tile);
            }
        }
        return toReturn.ToArray();
    }

    /// <summary>
    /// Get all units tiles to the provided one, especially usefull for large and huge units. 
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public List<UnitBase> GetAdjacentUnits(UnitBase unit) {
        var tiles = new List<Vector2Int>(); 

        if (unit.isHugeUnit) {
            tiles.AddRange(GetAdjacentTiles(unit.GetCurrentLocation(0)));
            tiles.AddRange(GetAdjacentTiles(unit.GetCurrentLocation(1)));
            tiles.AddRange(GetAdjacentTiles(unit.GetCurrentLocation(2)));
        }
        else if (unit.isLargeUnit) {
            tiles.AddRange(GetAdjacentTiles(unit.GetCurrentLocation(0)));
            tiles.AddRange(GetAdjacentTiles(unit.GetCurrentLocation(1)));
        } else {
            tiles.AddRange(GetAdjacentTiles(unit.GetCurrentLocation(0)));
        }

        var toReturn = new List<UnitBase>(); 

        for (int i = 0; i < tiles.Count; i++) {
            var u = GetUnitOnTile(tiles[i]); 
            if (u != unit && !toReturn.Contains(u)) {
                toReturn.Add(u); 
            }
        }

        return toReturn; 
    }

    /// <summary>
    /// Returns a 2 tiles long cone. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public Vector2Int[] GetShortCone(Vector2Int from, Vector2Int destination) {
        var middleLine = GetLine(from, destination, 2);
        var endPoint = middleLine[middleLine.Length - 1];
        var generalizedEndPoint = Generalize(endPoint, from);

        var toReturn = new List<Vector2Int>(); 

        if (generalizedEndPoint.x == 0) {            
            if (endPoint.y > from.y) {
                AddToListIfExist(toReturn, -2 + from.x, 2 + from.y);
                AddToListIfExist(toReturn, 0 + from.x, 2 + from.y);
                AddToListIfExist(toReturn, 2 + from.x, 2 + from.y);
                AddToListIfExist(toReturn, -1 + from.x, 1 + from.y);
                AddToListIfExist(toReturn, 1 + from.x, 1 + from.y);
            }
            else {
                AddToListIfExist(toReturn, -2 + from.x, -2 + from.y);
                AddToListIfExist(toReturn, 0 + from.x, -2 + from.y);
                AddToListIfExist(toReturn, 2 + from.x, -2 + from.y);
                AddToListIfExist(toReturn, -1 + from.x, -1 + from.y);
                AddToListIfExist(toReturn, 1 + from.x, -1 + from.y);
            }
        }
        else if (generalizedEndPoint.x == 2) {
            if (endPoint.y > from.y) {
                if (endPoint.x > from.x) {
                    AddToListIfExist(toReturn, 0 + from.x, 2 + from.y);
                    AddToListIfExist(toReturn, 2 + from.x, 2 + from.y);
                    AddToListIfExist(toReturn, 3 + from.x, 1 + from.y);
                    AddToListIfExist(toReturn, 1 + from.x, 1 + from.y);
                }
                else {
                    AddToListIfExist(toReturn, 0 + from.x, 2 + from.y);
                    AddToListIfExist(toReturn, -2 + from.x, 2 + from.y);
                    AddToListIfExist(toReturn, -3 + from.x, 1 + from.y);
                    AddToListIfExist(toReturn, -1 + from.x, 1 + from.y);
                }
            }
            else {
                if (endPoint.x > from.x) {
                    AddToListIfExist(toReturn, 0 + from.x, -2 + from.y);
                    AddToListIfExist(toReturn, 2 + from.x, -2 + from.y);
                    AddToListIfExist(toReturn, 3 + from.x, -1 + from.y);
                    AddToListIfExist(toReturn, 1 + from.x, -1 + from.y);
                }
                else {
                    AddToListIfExist(toReturn, 0 + from.x, -2 + from.y);
                    AddToListIfExist(toReturn, -2 + from.x, -2 + from.y);
                    AddToListIfExist(toReturn, -3 + from.x, -1 + from.y);
                    AddToListIfExist(toReturn, -1 + from.x, -1 + from.y);
                }
            }
        }
        else if (generalizedEndPoint.x == 3) {
            if (endPoint.y > from.y) {
                if (endPoint.x > from.x) {
                    AddToListIfExist(toReturn, 3 + from.x, 1 + from.y);
                    AddToListIfExist(toReturn, 1 + from.x, 1 + from.y);
                    AddToListIfExist(toReturn, 2 + from.x, 0 + from.y);
                    AddToListIfExist(toReturn, 2 + from.x, 2 + from.y);
                    AddToListIfExist(toReturn, 4 + from.x, 0 + from.y);
                }
                else {
                    AddToListIfExist(toReturn, -3 + from.x, 1 + from.y);
                    AddToListIfExist(toReturn, -1 + from.x, 1 + from.y);
                    AddToListIfExist(toReturn, -2 + from.x, 0 + from.y);
                    AddToListIfExist(toReturn, -2 + from.x, 2 + from.y);
                    AddToListIfExist(toReturn, -4 + from.x, 0 + from.y);
                }
            }
            else {
                if (endPoint.x > from.x) {
                    AddToListIfExist(toReturn, 3 + from.x, -1 + from.y);
                    AddToListIfExist(toReturn, 1 + from.x, -1 + from.y);
                    AddToListIfExist(toReturn, 2 + from.x, 0 + from.y);
                    AddToListIfExist(toReturn, 2 + from.x, -2 + from.y);
                    AddToListIfExist(toReturn, 4 + from.x, 0 + from.y);
                }
                else {
                    AddToListIfExist(toReturn, -3 + from.x, -1 + from.y);
                    AddToListIfExist(toReturn, -1 + from.x, -1 + from.y);
                    AddToListIfExist(toReturn, -2 + from.x, 0 + from.y);
                    AddToListIfExist(toReturn, -2 + from.x, -2 + from.y);
                    AddToListIfExist(toReturn, -4 + from.x, 0 + from.y);
                }
            }
        }
        else {
            if (endPoint.x > from.x) {
                AddToListIfExist(toReturn, 4 + from.x, 0 + from.y);
                AddToListIfExist(toReturn, 3 + from.x, 1 + from.y);
                AddToListIfExist(toReturn, 2 + from.x, 0 + from.y);
                AddToListIfExist(toReturn, 3 + from.x, -1 + from.y);
            }
            else {
                AddToListIfExist(toReturn, -4 + from.x, 0 + from.y);
                AddToListIfExist(toReturn, -3 + from.x, 1 + from.y);
                AddToListIfExist(toReturn, -2 + from.x, 0 + from.y);
                AddToListIfExist(toReturn, -3 + from.x, -1 + from.y);
            }
        }

        return toReturn.ToArray(); 
    }

    /// <summary>
    /// Returns a sphere. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public Vector2Int[] GetSphere(Vector2Int from, int radius, int minDistance = 0) {
        List<Vector2Int> toReturn = new List<Vector2Int>();
        foreach (var point in combinedMap) {
            int dist = GetDistance(from, point); 
            if (dist <= radius && dist >= minDistance) {
                toReturn.Add(point);
            }
        }
        return toReturn.ToArray();
    }

    /// <summary>
    /// Returns a line, this line can go beyond the destination point, if provided length is higher. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="destination"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public Vector2Int[] GetLine(Vector2Int from, Vector2Int destination, int? length = null, bool checkTileExists = true) {
        int distance = GetDistance(from, destination); 
        List<Vector2Int> toReturn = new List<Vector2Int>(); 
        for (float i = 1; i <= distance; i++) {
            float step = i / distance - 0.0001f; 
            float f = Mathf.Lerp((float)from.y * 10, (float)destination.y * 10, step); 
            int y = Mathf.RoundToInt(f / 10); 
            f = Mathf.Lerp((float)from.x * 10, (float)destination.x * 10, step); 
            float x = f / 10;
            // Adjust for double width 
            //Debug.Log("Cord1: (" + x + "." + y + ")");
            if (i == 0) Debug.Log("T: " + x); 
            var toAdd = CreateGridVector(x, y);
            if (checkTileExists) {
                if (TileExists(toAdd)) {
                    toReturn.Add(toAdd);
                }
            }
            else {
                toReturn.Add(toAdd);
            }            
        }

        // Extend the line further than the destination. 
        if (length != null) {
            if (length > toReturn.Count) {
                float x = toReturn[0].x - from.x + toReturn[toReturn.Count - 1].x;
                int y = toReturn[0].y - from.y + toReturn[toReturn.Count - 1].y;
                var toAdd = CreateGridVector(x, y);
                if (checkTileExists) {
                    if (TileExists(toAdd)) {
                        toReturn.Add(toAdd);
                    }
                }
                else {
                    toReturn.Add(toAdd);
                }
                int i = 0;
                while (length > toReturn.Count) {
                    x = toReturn[i + 1].x - toReturn[i].x + toReturn[toReturn.Count - 1].x;
                    y = toReturn[i + 1].y - toReturn[i].y + toReturn[toReturn.Count - 1].y;
                    toAdd = CreateGridVector(x, y);
                    if (checkTileExists) {
                        if (TileExists(toAdd)) {
                            toReturn.Add(toAdd);
                        }
                    }
                    else {
                        toReturn.Add(toAdd);
                    }
                    i++;
                }
            }
            else if (length < toReturn.Count) {
                Debug.Log("CUT " + length + ": " + toReturn.Count);
                toReturn.RemoveRange((int)length, toReturn.Count - (int)length);                
            }
        }        

        return toReturn.ToArray();
    }

    /// <summary>
    /// Returns a three tile large triangle. 
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public Vector2Int[] GetTriangle(Vector2Int destination, bool pointUp = true) {
        Vector2Int[] toReturn = null;
        Vector2Int t2 = new Vector2Int(destination.x + 2, destination.y);
        bool b2 = TileExists(t2.x, t2.y);
        Vector2Int t3 = new Vector2Int(destination.x + 1, destination.y + (pointUp? 1 : -1));
        bool b3 = TileExists(t3.x, t3.y);
        if (b2 && b3) {
            toReturn = new Vector2Int[] { destination, t2, t3 };
        }
        else if (!b2 && !b3) {
            toReturn = new Vector2Int[] { destination };
        }
        else if (b2) {
            toReturn = new Vector2Int[] { destination, t2 };
        }
        else {
            toReturn = new Vector2Int[] { destination, t3 };
        }
        return toReturn;
    }

    /// <summary>
    /// Returns the distance between the two points. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public int GetDistance(Vector2Int from, Vector2Int destination) {
        Vector2Int genVec = Generalize(from, destination);
        return genVec.y + Mathf.Max(0, (genVec.x - genVec.y) / 2);
    }

    /// <summary>
    /// Returns true if the tile exists in the current combat map. 
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public bool TileExists(Vector2Int tile) {
        return TileExists(tile.x, tile.y); 
    }

    /// <summary>
    /// Returns true if the tile exists in the current combat map. 
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public bool TileExists(int x, int y) {
        foreach (var item in combinedMap) {
            if (item.x == x && item.y == y) {
                return true;
            }
        }
        return false;
    }

    private void ConcatArray(ref Vector2Int[] source, Vector2Int[] toAdd) {
        int oldLength = source.Length;
        System.Array.Resize<Vector2Int>(ref source, oldLength + toAdd.Length);
        System.Array.Copy(toAdd, 0, source, oldLength, toAdd.Length);
    }

    /// <summary>
    /// Returns the positive generalized vector, between the two provided points. 
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    private Vector2Int Generalize(Vector2Int v1, Vector2Int v2) {
        int x = Mathf.Abs(v1.x - v2.x);
        int y = Mathf.Abs(v1.y - v2.y);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// This adjusts the X, acording to the rules of double width hexagon grid. 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private Vector2Int CreateGridVector(float x, int y) {
        if (Mathf.Abs(y) % 2 == 0) {
            int intX = Mathf.RoundToInt(x * 0.5f) * 2;
            return new Vector2Int(intX, y);
        }
        else {
            int intX = (2 * Mathf.FloorToInt(Mathf.Abs(x) / 2) + 1) * (x > 0 ? 1 : -1);
            return new Vector2Int(intX, y);
        }
    }

    /// <summary>
    /// Adds a vector2Int to the list, with provided cords, if tile exists. 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void AddToListIfExist(List<Vector2Int> list, float x, int y) {
        var toAdd = CreateGridVector(x, y); 
        if (toAdd != null) {
            list.Add(toAdd);
        }
    }

    /// <summary>
    /// This is used for Unity engine. 
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public Vector3 GetRealWorldLocation(Vector2Int tile) {
        return gridRealWorldRelation[tile]; 
    }

    /// <summary>
    /// Use this when inserting a unit into the grid. 
    /// If moving, remember to use RemoveUnitGridRelation first. 
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="unit"></param>
    public void SetGridUnitRelation(Vector2Int tile, UnitBase unit) {
        gridUnitRelation[tile] = unit; 
    }

    /// <summary>
    /// Use this when inserting an object into the grid. 
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="obstacle"></param>
    public void SetGridObstacleRelation(Vector2Int tile, TerrainObstacle obstacle) {
        gridObstacleRelation[tile] = obstacle;
    }

    /// <summary>
    /// Remove all entries of the provided unit, in grid reference. 
    /// Use this when moving, before inserting into grid again. 
    /// </summary>
    /// <param name="unit"></param>
    public void RemoveUnitGridRelation(UnitBase unit) {
        var toRemove = gridUnitRelation.Where(kvp => kvp.Value == unit).ToList();
        foreach (var item in toRemove) {
            gridUnitRelation.Remove(item.Key);
        }
    }

    /// <summary>
    /// Removes all entries of the provided obstacle, in grid reference. 
    /// </summary>
    /// <param name="obstacle"></param>
    public void RemoveObstacleGridRelation(TerrainObstacle obstacle) {
        var toRemove = gridObstacleRelation.Where(kvp => kvp.Value == obstacle).ToList(); 
        foreach (var item in toRemove) {
            gridObstacleRelation.Remove(item.Key);
        }
    }

    /// <summary>
    /// Returns the refenced unit in the grid dictionary. 
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public UnitBase GetUnitOnTile(Vector2Int tile) {
        if (gridUnitRelation.ContainsKey(tile)) {
            return gridUnitRelation[tile];
        }
        return null;
    }

    /// <summary>
    /// Returns the refenced obstacle in the grid dictionary. 
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public TerrainObstacle GetObstacleOnTile(Vector2Int tile) {
        if (gridObstacleRelation.ContainsKey(tile)) {
            return gridObstacleRelation[tile];
        }
        return null; 
    }

    /*
    /// <summary>
    /// This returns the tile for ass2, for huge units. Does NOT check for the tiles existence. 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="assTile1"></param>
    /// <returns></returns>
    public Vector2Int CalcHugePlacement(Vector2Int position, Vector2Int assTile1) {
        if (position.x > assTile1.x) {
            if (position.y < assTile1.y) {
                // 1
                return new Vector2Int(assTile1.x + 2, assTile1.y); 
            } else if (position.y > assTile1.y) {
                // 5
                return new Vector2Int(assTile1.x - 1, assTile1.y + 1);
            } else {
                // 6
                return new Vector2Int(assTile1.x + 1, assTile1.y + 1);
            }
        } else {
            if (position.y < assTile1.y) {
                // 2
                return new Vector2Int(assTile1.x + 1, assTile1.y - 1);
            } else if (position.y > assTile1.y) {
                // 4
                return new Vector2Int(assTile1.x - 2, assTile1.y);
            } else {
                // 3
                return new Vector2Int(assTile1.x  - 1, assTile1.y - 1);
            }
        }

        //  1)  5,3 / 4,4 = 6,4     x = ax + 2, y = ay          x > ax, y < yx
        //  2)  5,3 / 6,4 = 7,3     x = ax + 1, y = ay - 1      x < ax, y < yx
        //  3)  5,3 / 7,3 = 6,2     x = ax - 1, y = ay - 1      x < ax, y = yx
        //  4)  5,3 / 6,2 = 4,2     x = ax - 2, y = ay          x < ax, y > yx
        //  5)  5,3 / 4,2 = 3,3     x = ax - 1, y = ay + 1      x > ax, y > yx
        //  6)  5,3 / 3,3 = 4,4     x = ax + 1, y = ay + 1      x > ax, y = yx

        //if (position.y < assTile1.y) {
        //    if (position.x > assTile1.x) {
        //        return new Vector2Int(position.x + 2, position.y); 
        //    } else {
        //        return new Vector2Int(assTile1.x + 2, assTile1.y);
        //    }
        //}
        //else if (position.y > assTile1.y) {
        //    if (position.x > assTile1.x) {
        //        return new Vector2Int(assTile1.x - 2, assTile1.y);
        //    } else {
        //        return new Vector2Int(position.x - 2, position.y);
        //    }
        //} else {
        //    if (position.x > assTile1.x) {
        //        return new Vector2Int(position.x + 1, position.y - 1);
        //    } else {
        //        return new Vector2Int(position.x - 1, position.y + 1);
        //    }
        //}
    }
    */

    /// <summary>
    /// Returns true if the position is possible for a huge unit. 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="assTile1"></param>
    /// <returns></returns>
    public bool CheckHugePlacementPosible(Vector2Int position) {
        return (
            combatManager.CheckTileIsEmpty(position) && 
            combatManager.CheckTileIsEmpty(new Vector2Int(position.x - 1, position.y - 1)) && 
            combatManager.CheckTileIsEmpty(new Vector2Int(position.x + 1, position.y - 1))
            ); 
    }

    /*
    /// <summary>
    /// Returns null, if the movement is not possible. 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="assTile1"></param>
    /// <returns></returns>
    public Vector2Int? CalcHugePosiblePlacement(Vector2Int position, Vector2Int assTile1) {
        Vector2Int toReturn; 

        if (position.y < assTile1.y) {
            if (position.x > assTile1.x) {
                toReturn = new Vector2Int(position.x + 2, position.y);
            } else {
                toReturn = new Vector2Int(assTile1.x + 2, assTile1.y);
            }
        } else if (position.y > assTile1.y) {
            if (position.x > assTile1.x) {
                toReturn = new Vector2Int(assTile1.x - 2, assTile1.y);
            } else {
                toReturn = new Vector2Int(position.x - 2, position.y);
            }
        } else {
            if (position.x > assTile1.x) {
                toReturn = new Vector2Int(position.x + 1, position.y - 1);
            } else {
                toReturn = new Vector2Int(position.x - 1, position.y + 1);
            }
        }

        if (combatManager.CheckTileIsEmpty(toReturn)) {
            return toReturn; 
        } else {
            return null; 
        }
    }
    */
}
