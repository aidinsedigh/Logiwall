import { createSlice } from '@reduxjs/toolkit';

//---------------------------------------------------------------------------

const initialState = {
  trackingData: null,
};

const TrackingSlice = createSlice({
  name: 'TrackingSlice',
  initialState: initialState,
  reducers: {
    setTrackData: (state, action) => {
      state.trackingData = action.payload;
    },
  },
});
export const { setTrackData } = TrackingSlice.actions;
export default TrackingSlice.reducer;
