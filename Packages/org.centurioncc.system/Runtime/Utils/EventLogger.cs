using System;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EventLogger : UdonSharpBehaviour
    {
        private const int TupleValueLength = 5;

        [SerializeField]
        private GameObject visualizeObject;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PrintableBase logger;

        private readonly string _prefix = "EventLogger::";
        [UdonSynced]
        private Vector3 _hitDamagerContactPoint;
        [UdonSynced]
        private long _hitDamagerDateTimeLong;

        [UdonSynced]
        private Vector3 _hitDamagerOriginPos;
        [UdonSynced]
        private int _hitDamagerPlayerId;
        [UdonSynced]
        private int _hitPlayerId;

        [NonSerialized]
        public bool ShouldVisualizeOnLog;

        public string PersistentHitLogData { get; private set; } = "";
        public string TempHitLogData { get; private set; } = "";
        public string ShotLogData { get; private set; } = "";

        public void LogHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
            if (playerCollider == null || damageData == null)
                return;

            if (!damageData.ShouldApplyDamage)
                return;

            var localPlayerId = Networking.LocalPlayer.playerId;
            var hitPlayer = playerCollider.player;

            if ((_hitPlayerId != localPlayerId && damageData.DamagerPlayerId != localPlayerId) ||
                _hitPlayerId == damageData.DamagerPlayerId ||
                Networking.GetNetworkDateTime().Subtract(hitPlayer.LastHitData.HitTime).TotalSeconds < 5F)
                return;

            _hitPlayerId = playerCollider.player.PlayerId;
            _hitDamagerDateTimeLong = playerCollider.player.LastHitData.HitTimeTicks;
            _hitDamagerPlayerId = damageData.DamagerPlayerId;
            _hitDamagerOriginPos = damageData.DamageOriginPosition;
            _hitDamagerContactPoint = contactPoint;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            LogHit();
        }

        public override void OnPreSerialization()
        {
            LogHit();
        }

        private void LogHit()
        {
            var hitTime = new DateTime(_hitDamagerDateTimeLong);

            var hitPlayerApi = VRCPlayerApi.GetPlayerById(_hitPlayerId);
            var firedPlayerApi = VRCPlayerApi.GetPlayerById(_hitDamagerPlayerId);

            var hitPlayerName = TryGetDisplayName(hitPlayerApi);
            var shooterPlayerName = TryGetDisplayName(firedPlayerApi);

            var record =
                $"\n{ToHitLogCsvRecord(_hitDamagerContactPoint, _hitDamagerOriginPos, hitPlayerName, shooterPlayerName, hitTime)}";

            TempHitLogData += record;
            PersistentHitLogData += record;

            if (ShouldVisualizeOnLog)
            {
                VisualizeLine(_hitDamagerContactPoint, _hitDamagerOriginPos, hitPlayerName, shooterPlayerName, hitTime);
            }
        }

        public void LogShot(ManagedGun instance, ProjectileBase projectile)
        {
            if (instance == null)
            {
                Debug.LogError($"{_prefix}Tried to log shot but no instance found");
                return;
            }

            if (projectile == null) Debug.LogError($"{_prefix}Tried to log shot but no projectile found");

            var shooterPos = instance.ShooterPosition;
            var shooterRot = instance.ShooterRotation;
            var shooterVariantUniqueId = -1;
            if (instance) shooterVariantUniqueId = instance.VariantDataUniqueId;

            ShotLogData = string.Join("\n", ShotLogData,
                ToShotLogCsvRecord(shooterPos, shooterRot, shooterVariantUniqueId));
        }

        public void Visualize()
        {
            logger.Log($"{_prefix}visualizing record...");
            var records = CsvUtil.ParseCsvStringToRecords(TempHitLogData);
            foreach (var record in records)
            {
                logger.Log($"{_prefix}record: {record}");
                var recordParsed = CsvUtil.ParseCsvRecordToData(record, TupleValueLength);
                VisualizeLine(
                    CsvUtil.ParseVector3(recordParsed[0]),
                    CsvUtil.ParseVector3(recordParsed[1]),
                    recordParsed[2],
                    recordParsed[3],
                    CsvUtil.ParseDateTime(recordParsed[4])
                );
            }

            logger.Log($"{_prefix}visualization complete");
        }

        public void RemoveVisualization()
        {
            for (var i = 0; i < transform.childCount; i++) Destroy(transform.GetChild(i).gameObject);

            logger.Log($"{_prefix}removed visualization");
        }

        public void WriteLog()
        {
            logger.Log($"{_prefix} -- Begin Hit Log --");
            logger.Log(PersistentHitLogData);
            logger.Log($"{_prefix} -- End Hit Log --");
            logger.Log($"{_prefix} -- Begin Shot Log --");
            logger.Log(ShotLogData);
            logger.Log($"{_prefix} -- End Shot Log --");
        }

        public void ClearHitLog()
        {
            TempHitLogData = "";
        }

        public void ClearShotLog()
        {
            ShotLogData = "";
        }

        public void ClearLog()
        {
            ClearHitLog();
            ClearShotLog();
            logger.Log($"{_prefix}cleared all log data");
        }

        private void VisualizeLine(Vector3 v1, Vector3 v2, string hitPlayer, string shooterPlayer, DateTime time)
        {
            Debug.Log($"{_prefix}visualizing line at {v1} to {v2}");

            var obj = Instantiate(visualizeObject, transform, true);

            var lineRenderer = obj.GetComponent<LineRenderer>();

            var positions = new[] { v1, v2 };

            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);

            var texts = obj.GetComponentsInChildren<TextMeshPro>();
            if (texts.Length != 3)
                return;

            var hitterText = texts[0];
            var shooterText = texts[1];
            var infoText = texts[2];

            var textRotation = Quaternion.LookRotation(v1 - v2) * Quaternion.Euler(0, -90, 0);

            hitterText.text = $"Hit: {hitPlayer}\n";
            hitterText.gameObject.transform.SetPositionAndRotation(v1, textRotation);

            shooterText.text = $"Shooter: {shooterPlayer}\n";
            shooterText.gameObject.transform.SetPositionAndRotation(v2, textRotation);

            infoText.text = $"Distance: {Vector3.Distance(v1, v2):F2}m\n" +
                            $"Time: {time:HH:mm:ss.fff}";
            infoText.gameObject.transform.SetPositionAndRotation(
                Vector3.MoveTowards(v1, v2, Vector3.Distance(v1, v2) / 2),
                textRotation);
        }

        private string ToHitLogCsvRecord(Vector3 v1, Vector3 v2, string s1, string s2, DateTime t)
        {
            return
                $"{CsvUtil.ToCsvString(v1)},{CsvUtil.ToCsvString(v2)},\"{s1}\",\"{s2}\",{t.Ticks}";
        }

        private string ToShotLogCsvRecord(Vector3 v1, Quaternion q1, int i1)
        {
            return $"{CsvUtil.ToCsvString(v1)},{CsvUtil.ToCsvString(q1)},{i1},{DateTime.UtcNow.Ticks}";
        }

        private string TryGetDisplayName(VRCPlayerApi api)
        {
            if (api == null || !api.IsValid())
                return "Dummy (invalid player)";
            return api.displayName;
        }
    }
}