using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTANetwork.Util;
using GTANetwork.Sync;
using Vector3 = GTA.Math.Vector3;
using System.Diagnostics;

namespace GTANetwork.Streamer
{
    internal class StreamerThread : Script
    {
        public static SyncPed[] SyncPeds;
        public static SyncPed[] StreamedInPlayers;
        public static RemoteVehicle[] StreamedInVehicles;

        private static List<IStreamedItem> _itemsToStreamIn;
        private static List<IStreamedItem> _itemsToStreamOut;

        public static Stopwatch Sw;

        public StreamerThread()
        {
            _itemsToStreamIn = new List<IStreamedItem>();
            _itemsToStreamOut = new List<IStreamedItem>();
            StreamedInPlayers = new SyncPed[MAX_PLAYERS];

            Tick += StreamerTick;

            var calcucationThread = new System.Threading.Thread(StreamerCalculationsThread) { IsBackground = true };
            calcucationThread.Start();
        }

        private static Vector3 _playerPosition;

        public const int MAX_PLAYERS = 250; //Max engine ped value: 256, on 236 it starts to cause issues //Global
        public const int MAX_OBJECTS = 500; //Max engine value: 2500 //Close
        public const int MAX_VEHICLES = 80; //Max engine value: 64 +/ 1 max 128 //Global
        public const int MAX_PICKUPS = 50; //NEEDS A TEST //VeryClose
        public const int MAX_BLIPS = 50; //Max engine value: 1298 //All
        public static int MAX_PEDS; //Share the Ped limit, prioritize the players //100 2x2 - 150
        public const int MAX_LABELS = MAX_PLAYERS; //NEEDS A TEST //VeryClose
        public const int MAX_MARKERS = 120; //Max engine value: 128 //VeryClose
        public const int MAX_PARTICLES = 50; //VeryClose

        private const float GlobalRange = 2000f; //2000
        private const float MediumRange = 100f; //1000
        private const float CloseRange = 500f; //500
        private const float VeryCloseRange = 100f; //100

        private const float GlobalRangeSquared = GlobalRange * GlobalRange;
        private const float MediumRangeSquared = MediumRange * MediumRange;
        private const float CloseRangeSquared = CloseRange * CloseRange;
        private const float VeryCloseRangeSquared = VeryCloseRange * VeryCloseRange;

        private static void StreamerCalculationsThread()
        {
            while (true)
            {
                if (!Main.IsOnServer() || !Main.HasFinishedDownloading)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
                
                var position = _playerPosition.ToLVector();

                IStreamedItem[] rawMap;
                lock (Main.NetEntityHandler.ClientMap) rawMap = Main.NetEntityHandler.ClientMap.Values.Where(item => !(item is RemotePlayer) || ((RemotePlayer) item).LocalHandle != -2).ToArray();
                
                #region Players
                SyncPeds = rawMap.OfType<SyncPed>().ToArray();
                //var streamedInPlayers = SyncPeds.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position.ToLVector(), GlobalRangeSquared)).ToArray();
                
                var streamedInPlayerList = new List<SyncPed>();  
                foreach (var item in SyncPeds)
                {
                    if (IsInRangeSquared(position, item.Position.ToLVector(), VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPlayerList.Add(item);
                }
                  
                foreach (var item in SyncPeds)
                {
                    if (streamedInPlayerList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position.ToLVector(), CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPlayerList.Add(item);
                }
                
                foreach (var item in SyncPeds)
                {
                    if (streamedInPlayerList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position.ToLVector(), MediumRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPlayerList.Add(item);
                }
                
                foreach (var item in SyncPeds)
                {
                    if (streamedInPlayerList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position.ToLVector(), GlobalRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPlayerList.Add(item);
                }
                var streamedInPlayers = streamedInPlayerList.ToArray();
                
                lock (_itemsToStreamIn)
                {
                    _itemsToStreamIn.AddRange(streamedInPlayers.Take(MAX_PLAYERS).Where(item => !item.StreamedIn));
                }
                lock (StreamedInPlayers)
                {
                    StreamedInPlayers = streamedInPlayers.Take(MAX_PLAYERS).ToArray();
                }

                var streamedOutPlayers = SyncPeds.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0 || !IsInRangeSquared(position, item.Position.ToLVector(), GlobalRangeSquared)) && item.StreamedIn);
                
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInPlayers.Skip(MAX_PLAYERS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutPlayers);
                }
                #endregion

                var entityMap = rawMap.Where(item => item.Position != null).ToArray();

                #region Vehicles
                var vehicles = entityMap.OfType<RemoteVehicle>().ToArray();
                
                //StreamedInVehicles = Vehicles.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, GlobalRangeSquared)).ToArray();
                
                var streamedInVehiclesList = new List<RemoteVehicle>();  
                foreach (var item in vehicles)
                {
                    if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInVehiclesList.Add(item);
                }
                  
                foreach (var item in vehicles)
                {
                    if (streamedInVehiclesList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInVehiclesList.Add(item);
                }
                
                foreach (var item in vehicles)
                {
                    if (streamedInVehiclesList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, MediumRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInVehiclesList.Add(item);
                }
                
                foreach (var item in vehicles)
                {
                    if (streamedInVehiclesList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, GlobalRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInVehiclesList.Add(item);
                }
                StreamedInVehicles = streamedInVehiclesList.ToArray();
                
                lock (_itemsToStreamIn)
                {
                    _itemsToStreamIn.AddRange(StreamedInVehicles.Take(MAX_VEHICLES).Where(item => !item.StreamedIn));
                }

                //var streamedOutVehicles = Vehicles.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0) || !IsInRangeSquared(position, item.Position, GlobalRangeSquared) && item.StreamedIn);
                var streamedOutVehicles = vehicles.Where(item => !IsInRangeSquared(position, item.Position, GlobalRangeSquared) && item.StreamedIn || Main.LocalDimension != item.Dimension || item.Dimension != 0).ToArray();
                  
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(StreamedInVehicles.Skip(MAX_VEHICLES).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutVehicles);
                }
                #endregion

                #region Objects
                var objects = entityMap.OfType<RemoteProp>().ToArray();

                //var streamedInObjects = objects.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, GlobalRangeSquared)).ToArray();
                
                var streamedInObjectList = new List<RemoteProp>();  
                foreach (var item in objects)
                {
                    if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInObjectList.Add(item);
                }
                  
                foreach (var item in objects)
                {
                    if (streamedInObjectList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInObjectList.Add(item);
                }
                
                foreach (var item in objects)
                {
                    if (streamedInObjectList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, MediumRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInObjectList.Add(item);
                }
                
                foreach (var item in objects)
                {
                    if (streamedInObjectList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, GlobalRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInObjectList.Add(item);
                }
                var streamedInObjects = streamedInObjectList.ToArray();
                
                lock (_itemsToStreamIn)
                {
                    _itemsToStreamIn.AddRange(streamedInObjects.Take(MAX_OBJECTS).Where(item => !item.StreamedIn));
                }

                var streamedOutObjects = objects.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0 || !IsInRangeSquared(position, item.Position, GlobalRangeSquared)) && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInObjects.Skip(MAX_OBJECTS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutObjects);
                }
                #endregion

                #region Other Shit
                var markers = entityMap.OfType<RemoteMarker>().ToArray();
                //var streamedInMarkers = markers.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, GlobalRangeSquared)).ToArray();
                
                var streamedInMarkerList = new List<RemoteMarker>();  
                foreach (var item in markers)
                {
                    if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInMarkerList.Add(item);
                }
                  
                foreach (var item in markers)
                {
                    if (streamedInMarkerList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInMarkerList.Add(item);
                }
                
                foreach (var item in markers)
                {
                    if (streamedInMarkerList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, MediumRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInMarkerList.Add(item);
                }
                var streamedInMarkers = streamedInMarkerList.ToArray();
                
                lock (_itemsToStreamIn) _itemsToStreamIn.AddRange(streamedInMarkers.Take(MAX_MARKERS).Where(item => !item.StreamedIn));

                var streamedOutMarkers = markers.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0) || !IsInRangeSquared(position, item.Position, GlobalRangeSquared) && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInMarkers.Skip(MAX_MARKERS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutMarkers);
                }


                var peds = entityMap.OfType<RemotePed>().ToArray();
                MAX_PEDS = MAX_PLAYERS - streamedInPlayers.Take(MAX_PLAYERS).Count();
                //var streamedInPeds = peds.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, GlobalRangeSquared)).ToArray();
                
                var streamedInPedList = new List<RemotePed>();  
                foreach (var item in peds)
                {
                    if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPedList.Add(item);
                }
                
                foreach (var item in peds)
                {
                    if (streamedInPedList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPedList.Add(item);
                }
                
                foreach (var item in peds)
                {
                    if (streamedInPedList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, MediumRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPedList.Add(item);
                }
                
                foreach (var item in peds)
                {
                    if (streamedInPedList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, GlobalRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInPedList.Add(item);
                }
                var streamedInPeds = streamedInPedList.ToArray();
                
                lock (_itemsToStreamIn) _itemsToStreamIn.AddRange(streamedInPeds.Take(MAX_PEDS).Where(item => !item.StreamedIn));

                var streamedOutPeds = peds.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0 || !IsInRangeSquared(position, item.Position, GlobalRangeSquared)) && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInPeds.Skip(MAX_PEDS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutPeds);
                }


                var labels = entityMap.OfType<RemoteTextLabel>().ToArray();
                //var streamedInLabels = labels.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, CloseRangeSquared)).ToArray();
                
                var streamedInLabelList = new List<RemoteTextLabel>();  
                foreach (var item in labels)
                {
                    if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInLabelList.Add(item);
                }
                
                foreach (var item in labels)
                {
                    if (streamedInLabelList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInLabelList.Add(item);
                }
                var streamedInLabels = streamedInLabelList.ToArray();
                
                lock (_itemsToStreamIn) _itemsToStreamIn.AddRange(streamedInLabels.Take(MAX_LABELS).Where(item => !item.StreamedIn));

                var streamedOutLabels = labels.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0 || !IsInRangeSquared(position, item.Position, CloseRangeSquared)) && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInLabels.Skip(MAX_LABELS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutLabels);
                }


                var particles = entityMap.OfType<RemoteParticle>().ToArray();
                //var streamedInParticles = particles.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, GlobalRangeSquared)).ToArray();
                
                var streamedInParticlesList = new List<RemoteParticle>();  
                foreach (var item in particles)
                {
                    if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInParticlesList.Add(item);
                }
                
                foreach (var item in particles)
                {
                    if (streamedInParticlesList.Contains(item)) continue;
                    if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                        streamedInParticlesList.Add(item);
                }
                var streamedInParticles = streamedInParticlesList.ToArray();
                
                lock (_itemsToStreamIn) _itemsToStreamIn.AddRange(streamedInParticles.Take(MAX_PARTICLES).Where(item => !item.StreamedIn));

                var streamedOutParticles = particles.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0) || !IsInRangeSquared(position, item.Position, GlobalRangeSquared) && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInParticles.Skip(MAX_PARTICLES).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutParticles);
                }


                var pickups = entityMap.OfType<RemotePickup>().ToArray();
                //var streamedInPickups = pickups.Where(item => (item.Dimension == Main.LocalDimension || item.Dimension == 0) && IsInRangeSquared(position, item.Position, MediumRangeSquared)).ToArray();

                var streamedInPickups = pickups.Where(item => IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0)).ToArray();
                lock (_itemsToStreamIn) _itemsToStreamIn.AddRange(streamedInPickups.Take(MAX_PICKUPS).Where(item => !item.StreamedIn));

                var streamedOutPickups = pickups.Where(item => (item.Dimension != Main.LocalDimension && item.Dimension != 0) || !IsInRangeSquared(position, item.Position, MediumRangeSquared) && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInPickups.Skip(MAX_PICKUPS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutPickups);
                }


                var blips = entityMap.OfType<RemoteBlip>().ToArray();
                var streamedInBlips = blips.Where(item => item.Dimension == Main.LocalDimension || item.Dimension == 0).ToArray();
                lock (_itemsToStreamIn) _itemsToStreamIn.AddRange(streamedInBlips.Take(MAX_BLIPS).Where(item => !item.StreamedIn));

                var streamedOutBlips = blips.Where(item => item.Dimension != Main.LocalDimension && item.Dimension != 0 && item.StreamedIn);
                lock (_itemsToStreamOut)
                {
                    _itemsToStreamOut.AddRange(streamedInBlips.Skip(MAX_BLIPS).Where(item => item.StreamedIn));
                    _itemsToStreamOut.AddRange(streamedOutBlips);
                }
                #endregion

                System.Threading.Thread.Sleep(500);
            }
        }

        private static void StreamerTick(object sender, EventArgs e)
        {
            _playerPosition = Game.Player.Character.Position;
            if (Util.Util.ModelRequest) return;
            Sw = new Stopwatch();

            if (DebugInfo.StreamerDebug) Sw.Start();

            lock (_itemsToStreamOut)
            {
                foreach (var t in _itemsToStreamOut)
                {
                    Main.NetEntityHandler.StreamOut(t);
                }
                _itemsToStreamOut.Clear();
            }

            lock (_itemsToStreamIn)
            {
                foreach (var t in _itemsToStreamIn)
                {
                    Main.NetEntityHandler.StreamIn(t);
                }
                _itemsToStreamIn.Clear();
            }

            if (DebugInfo.StreamerDebug) Sw.Stop();
        }

        private static bool IsInRange(GTANetworkShared.Vector3 center, GTANetworkShared.Vector3 dest, float range)
        {
            return center.Subtract(dest).Length() <= range;
        }

        private static bool IsInRangeSquared(GTANetworkShared.Vector3 center, GTANetworkShared.Vector3 dest, float range)
        {
            return center.Subtract(dest).LengthSquared() <= range;
        }
        
        /*private static object[] GetRange(List<> items, GTANetworkShared.Vector3 position)
        {
            var streamedInPedList = new List<RemotePed>();  
            foreach (var item in items)
            {
                if (IsInRangeSquared(position, item.Position, VeryCloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                    streamedInPedList.Add(item);
            }
                
            foreach (var item in items)
            {
                if (streamedInPedList.Contains(item)) continue;
                if (IsInRangeSquared(position, item.Position, CloseRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                    streamedInPedList.Add(item);
            }
                
            foreach (var item in items)
            {
                if (streamedInPedList.Contains(item)) continue;
                if (IsInRangeSquared(position, item.Position, MediumRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                    streamedInPedList.Add(item);
            }
                
            foreach (var item in items)
            {
                if (streamedInPedList.Contains(item)) continue;
                if (IsInRangeSquared(position, item.Position, GlobalRangeSquared) && (Main.LocalDimension == item.Dimension || item.Dimension == 0))
                    streamedInPedList.Add(item);
            }
            return streamedInPedList.ToArray();
        }*/
    }
}