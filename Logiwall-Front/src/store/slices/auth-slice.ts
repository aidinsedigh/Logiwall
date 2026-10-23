import { createSlice } from '@reduxjs/toolkit';

//---------------------------------------------------------------------------

const initialState = {
  authenticated: false,
  token: '',
  expTime: null,
  channel: '',
};

const AuthSlice = createSlice({
  name: 'AuthSlice',
  initialState: initialState,
  reducers: {
    setAuthStatus: (state, action) => {
      state.authenticated = action.payload;
    },
    setToken: (state, action) => {
      state.token = action.payload.token;
      state.expTime = action.payload.expTime;
    },
    setChannel: (state, action) => {
      state.channel = action.payload;
    },
  },
});
export const { setAuthStatus, setToken, setChannel } = AuthSlice.actions;
export default AuthSlice.reducer;
