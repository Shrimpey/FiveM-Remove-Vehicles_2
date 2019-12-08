// Uncomment #undefine line to suppress logging
#define DEBUG
//#undef DEBUG

using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace RemoveVehicles_2_0 {

    public class RemoveVehicles : BaseScript
    {
        #region Variables
        public DecoratorManager manager;
        private int currentClient_;
        private readonly string decorName_ = "rem_veh_id";
        private int netID = -1;

        // Experimental feature
        // Should script remove all the cars (not only the ones spawned by players)?
        // Warning: takes a lot of time if there are ped vehicles spawned all over the map
        private readonly bool removeNotOwnedCars = false;
        #endregion

        private void DebugLog(string entry) {
            #if (DEBUG)
            Debug.WriteLine("[VehRem] " + entry);
            #endif
        }

        //Constructor
        public RemoveVehicles(){
            // Get current player to register decorators for his vehicle
            currentClient_ = PlayerPedId();

            // Register custom event to be trigger from server once a player disconnects
            EventHandlers["eventDeleteVehicles"] += new System.Action<string>(DeleteVehiclesAsync);
            EventHandlers["playerConnected"] += new System.Action<string>(UpdateNetID);

            // Initialize decorator manager
            try {
                manager = new DecoratorManager(decorName_);
                manager.RegisterDecorator();
                DebugLog("Manager initialized!");
            }catch(System.Exception e) {
                DebugLog("Encountered an exception on initialization!");
                DebugLog(e.Message);
            }

            TriggerServerEvent("getNetID");

            Tick += CurrentVehicleUpdate;
        }

        // Updating NetID according to the scoreboard ID
        private void UpdateNetID(string playerSrc) {
            DebugLog("Updating NetID to " + playerSrc);
            netID = int.Parse(playerSrc, System.Globalization.NumberStyles.Any);
        }

        // Checking for new vehicle spawned by client
        private async Task CurrentVehicleUpdate() {
            // Get current player to register decorators for his vehicle
            currentClient_ = PlayerPedId();

            if (IsPedInAnyVehicle(currentClient_, false)) {
                int vehicle = GetVehiclePedIsIn(currentClient_, false);

                // Register decorator only if hasn't been registered yet
                if (manager.ReadDecoratorEntry(vehicle) == -1) {
                    if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == currentClient_) {
                        // Register decorator on client's car
                        int vehicleHandle = GetVehiclePedIsIn(currentClient_, false);
                        if (netID != -1) {
                            manager.WriteDecoratorEntry(vehicleHandle, netID);
                            DebugLog("Registered decorator " + netID.ToString());
                        } else {
                            DebugLog("NetID not registered properly!");
                        }
                    }
                }
            }
            await Task.FromResult(0);
        }

        // Same as GetAllVehicles() but returns list instead
        private List<Vehicle> GetAllVehiclesList() {
            List<Vehicle> vehicles = new List<Vehicle>();

            int entHandle = -1;
            int handle = FindFirstVehicle(ref entHandle);
            Vehicle veh = (Vehicle)Entity.FromHandle(entHandle);
            if (veh != null && veh.Exists())
                vehicles.Add(veh);

            entHandle = -1;
            while (FindNextVehicle(handle, ref entHandle)) {
                veh = (Vehicle)Entity.FromHandle(entHandle);
                if (veh != null && veh.Exists())
                    vehicles.Add(veh);
                entHandle = -1;
            }
            EndFindVehicle(handle);
            return vehicles;
        }

        // Triggers upon another client's disconnection, target is chosen randomly by server
        private async void DeleteVehiclesAsync(string playerSrc) {
            DebugLog("Triggered deleteVehicles event!");
            DebugLog("Player from source: " + playerSrc);
            int netHandle = int.Parse(playerSrc, System.Globalization.NumberStyles.Any);
            
            // Get all the vehicles
            List<Vehicle> vehicles = GetAllVehiclesList();

            #region Looking for vehicles to remove
            for (int i = vehicles.Count - 1; i >= 0; i--) {
                int decoratorEntry = manager.ReadDecoratorEntry(vehicles[i].Handle);
                DebugLog("Read decorator entry " + decoratorEntry.ToString());

                if ( decoratorEntry == netHandle) {
                    DebugLog("Found vehicle to remove with handle " + vehicles[i].Handle.ToString());
                }else if(decoratorEntry == -1 && removeNotOwnedCars) {
                    DebugLog("Found not owned vehicle to remove with handle " + vehicles[i].Handle.ToString());
                } else if(removeNotOwnedCars) {
                    vehicles.RemoveAt(i);   // Remove from "to remove" list
                    DebugLog("Decorator is owned by this client, not removing!");
                } else {
                    vehicles.RemoveAt(i);   // Remove from "to remove" list
                }
            }
            #endregion

            #region Removing vehicles
            // First request control of entites
            for (int i = vehicles.Count - 1; i >= 0; i--) {
                NetworkRequestControlOfEntity(vehicles[i].Handle);
            }

            // Proceed with deleting
            for (int i = vehicles.Count - 1; i >= 0; i--) {
                DebugLog("Removing " + vehicles[i].ClassLocalizedName.ToString() + ", " + vehicles[i].DisplayName.ToString() + "...");
                if (vehicles[i].DisplayName.ToString() != "CARNOTFOUND") {
                    // Prepare for deletion
                    int timeout = 15000;
                    while (!NetworkHasControlOfEntity(vehicles[i].Handle) && timeout > 0) {
                        timeout -= 100;
                        await Delay(100);
                    }

                    if (timeout > 0) {
                        vehicles[i].PreviouslyOwnedByPlayer = false;
                        SetEntityAsMissionEntity(vehicles[i].Handle, true, true);
                        // Finally delete the vehicle
                        vehicles[i].Delete();
                        vehicles.RemoveAt(i);
                        DebugLog("Removed properly!");
                    } else {
                        DebugLog("Timeout reached for getting control of entity, skipping vehicle deletion.");
                    }
                } else {
                    DebugLog("Car not found, skipping vehicle deletion.");
                }
            }
            vehicles.Clear();
            vehicles = null;
            #endregion
        }
    }
}
