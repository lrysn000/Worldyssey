using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 按季节生成地形模块：每个世界一套地面（网格加外围装饰地带，四角用圆角地块）、草和外围装饰，以及墙的变体挑选。
    /// 所有随机都来自 <see cref="CellRandom"/>，结果只由关卡数据决定。
    /// </summary>
    public static class SeasonTileBuilder
    {
        private const int SaltSharedWall = 1;
        private const int SaltUniqueWall = 2;
        private const int SaltGrass = 3;
        private const int SaltRim = 4;

        // 圆角模型朝右上；绕 y 轴每转 90°，圆角顺时针移到下一个角
        private const float TopRightAngle = 0f;
        private const float BottomRightAngle = 90f;
        private const float BottomLeftAngle = 180f;
        private const float TopLeftAngle = 270f;

        /// <summary>
        /// 在 <paramref name="root"/> 下为世界 <paramref name="world"/> 生成地面：整张网格（墙下也铺，岛上不留空洞）
        /// 加上外围 <see cref="LevelVisualConfig.RimWidth"/> 圈装饰地带。再撒草、摆外围装饰。
        /// </summary>
        public static void BuildGround(LevelVisualConfig config, LevelMap map, int world, Transform root)
        {
            SeasonVisualSet season = config.GetSeasonSet((int)map.GetWorldTheme(world));
            GameObject floor = season != null && season.FloorPrefab != null ? season.FloorPrefab : config.FloorPrefab;
            GameObject corner = season != null ? season.FloorCornerPrefab : null;
            int rim = config.RimWidth;
            int minX = -rim;
            int minY = -rim;
            int maxX = map.Width - 1 + rim;
            int maxY = map.Height - 1 + rim;
            var grassBatch = new GrassMeshBatch(root, season != null ? season.GrassMaterial : null);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var pos = new GridPos(x, y);
                    bool isRight = x == maxX;
                    bool isTop = y == maxY;
                    bool isCorner = corner != null && (x == minX || isRight) && (y == minY || isTop);
                    Spawn(isCorner ? corner : floor, root, config.GridToLocal(pos), Quaternion.Euler(0f, isCorner ? CornerAngle(isRight, isTop) : 0f, 0f));
                    if (season == null)
                    {
                        continue;
                    }

                    int ring = RingIndex(map, pos);
                    if (ring > 0)
                    {
                        SpawnRimDecor(season, root, config, world, pos, ring == rim);
                        AddGrass(season, grassBatch, config, world, pos);
                    }
                    else if (CanHaveGrass(map, world, pos))
                    {
                        AddGrass(season, grassBatch, config, world, pos);
                    }
                }
            }

            grassBatch.Build();
            FloorGridBuilder.Build(config, map, floor, config.FloorGridMaterial, root);
        }

        /// <summary>
        /// 共享墙的变体：配置了变体列表就按坐标挑一个，否则用占位预制体。不旋转，保证石头的轮廓在四个世界一致。
        /// </summary>
        public static GameObject PickSharedWall(LevelVisualConfig config, GridPos pos)
        {
            var random = new CellRandom(pos, -1, SaltSharedWall);
            return Pick(config.SharedWallVariants, ref random, config.SharedWallPrefab);
        }

        /// <summary>
        /// 独有墙的变体和朝向。<paramref name="seasonIndex"/> 决定用哪个季节的模块，<paramref name="world"/> 只用作随机种子。
        /// 季节没有配置独有墙时用占位预制体。
        /// </summary>
        public static GameObject PickUniqueWall(LevelVisualConfig config, int seasonIndex, int world, GridPos pos, out Quaternion rotation)
        {
            var random = new CellRandom(pos, world, SaltUniqueWall);
            SeasonVisualSet season = config.GetSeasonSet(seasonIndex);
            GameObject prefab = Pick(season != null ? season.UniqueWallPrefabs : null, ref random, config.UniqueWallPrefab);
            rotation = Quaternion.Euler(0f, random.Range(4) * 90f, 0f);
            return prefab;
        }

        /// <summary>
        /// 格子在网格外第几圈，网格内为 0。
        /// </summary>
        private static int RingIndex(LevelMap map, GridPos pos)
        {
            int outX = Mathf.Max(-pos.X, pos.X - (map.Width - 1));
            int outY = Mathf.Max(-pos.Y, pos.Y - (map.Height - 1));
            return Mathf.Max(0, Mathf.Max(outX, outY));
        }

        private static void SpawnRimDecor(SeasonVisualSet season, Transform root, LevelVisualConfig config, int world, GridPos pos, bool isOutermost)
        {
            var random = new CellRandom(pos, world, SaltRim);
            GameObject prefab = null;
            if (isOutermost && season.RimTallPrefabs.Count > 0 && random.Value() < season.RimTallChance)
            {
                prefab = season.RimTallPrefabs[random.Range(season.RimTallPrefabs.Count)];
            }
            else if (season.RimLowPrefabs.Count > 0 && random.Value() < season.RimLowChance)
            {
                prefab = season.RimLowPrefabs[random.Range(season.RimLowPrefabs.Count)];
            }

            if (prefab == null)
            {
                return;
            }

            // 装饰不在玩法格子上，可以比草更靠近格子边缘
            float halfRange = 0.25f * config.CellSize;
            var offset = new Vector3(random.Range(-halfRange, halfRange), 0f, random.Range(-halfRange, halfRange));
            GameObject decor = Spawn(prefab, root, config.GridToLocal(pos) + offset, Quaternion.Euler(0f, random.Range(0f, 360f), 0f));
            Vector2 scaleRange = season.RimScaleRange;
            decor.transform.localScale = Vector3.one * random.Range(scaleRange.x, scaleRange.y);
        }

        private static bool CanHaveGrass(LevelMap map, int world, GridPos pos)
        {
            // 终点和底座是玩法标识，墙格本身有模型，这些格子都不撒草
            return !map.IsSharedWall(pos) && !map.IsGoal(pos) && !map.IsPedestal(pos) && !map.IsUniqueWall(world, pos);
        }

        private static void AddGrass(SeasonVisualSet season, GrassMeshBatch batch, LevelVisualConfig config, int world, GridPos pos)
        {
            IReadOnlyList<GameObject> grass = season.GrassPrefabs;
            if (grass == null || grass.Count == 0)
            {
                return;
            }

            var random = new CellRandom(pos, world, SaltGrass);
            int count = season.GrassPerCellMin + random.Range(season.GrassPerCellMax - season.GrassPerCellMin + 1);
            float halfRange = (0.5f - season.GrassEdgeMargin) * config.CellSize;
            Vector3 center = config.GridToLocal(pos);
            Vector2 scaleRange = season.GrassScaleRange;
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = grass[random.Range(grass.Count)];
                var offset = new Vector3(random.Range(-halfRange, halfRange), 0f, random.Range(-halfRange, halfRange));
                if (prefab == null)
                {
                    continue;
                }

                batch.Add(prefab, center + offset, Quaternion.Euler(0f, random.Range(0f, 360f), 0f), random.Range(scaleRange.x, scaleRange.y));
            }
        }

        private static GameObject Pick(IReadOnlyList<GameObject> variants, ref CellRandom random, GameObject fallback)
        {
            if (variants == null || variants.Count == 0)
            {
                return fallback;
            }

            GameObject picked = variants[random.Range(variants.Count)];
            return picked != null ? picked : fallback;
        }

        private static float CornerAngle(bool isRight, bool isTop)
        {
            if (isRight)
            {
                return isTop ? TopRightAngle : BottomRightAngle;
            }

            return isTop ? TopLeftAngle : BottomLeftAngle;
        }

        private static GameObject Spawn(GameObject prefab, Transform root, Vector3 localPosition, Quaternion localRotation)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, root);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }
    }
}
