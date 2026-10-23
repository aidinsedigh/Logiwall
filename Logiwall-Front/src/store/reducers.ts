import AuthSlice from './slices/auth-slice';
import CreateOrderSlice from './slices/create-order-slice';
import TrackingSlice from './slices/tracking-slice';

//--------------------------------------------------------------------

const Reducers = {
  AuthSlice: AuthSlice,
  CreateOrderSlice: CreateOrderSlice,
  TrackingSlice: TrackingSlice,
};
export default Reducers;
