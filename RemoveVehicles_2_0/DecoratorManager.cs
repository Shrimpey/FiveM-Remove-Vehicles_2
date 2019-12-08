using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace RemoveVehicles_2_0 {

    public class DecoratorManager : BaseScript {

        #region Variables and constructors
        private string name_;

        // Constructor
        public DecoratorManager(string name) {
            name_ = name;
        }
        #endregion

        #region Decorator methods
        public void RegisterDecorator() {
            DecorRegister(name_, 1);
        }
        public bool WriteDecoratorEntry(int vehicleHandle, int ownerID) {
            if (ownerID != -1 && vehicleHandle != -1) {
                DecorSetInt(vehicleHandle, name_, ownerID);
                return true;
            } else {
                return false;
            }
        }
        public int ReadDecoratorEntry(int vehicleHandle) {
            if (DecorExistOn(vehicleHandle, name_) && vehicleHandle != -1) {
                return DecorGetInt(vehicleHandle, name_);
            } else {
                return -1;
            }
        }
        public bool RemoveDecorator(int vehicleHandle) {
            if (vehicleHandle != -1) {
                if (DecorExistOn(vehicleHandle, name_)) {
                    DecorRemove(vehicleHandle, name_);
                    return true;
                }
                return false;
            } else {
                return false;
            }
        }
        #endregion
    }

}
