import { createSlice } from '@reduxjs/toolkit';

//-----------------------------------------------------------------------

const initialState = {
  activeStep: 0,
  cities: { source: '', destination: '' },
  activeStepStatus: '',
  stepOneData: {
    source: 'تهران',
    destination: 'تفت',
  },
  stepTwoData: {},
  stepFourData: { senderLat: '', senderLng: '' },
  stepFiveData: {},
  stepSixData: { recieverLat: '', recieverLng: '' },
  stepSevenData: {},
};

const CreateOrderSlice = createSlice({
  name: 'CreateOrderSlice',
  initialState: initialState,
  reducers: {
    setActiveStep: (state, action) => {
      if (action.payload > state.activeStep) {
        state.activeStepStatus = 'increase';
      } else {
        state.activeStepStatus = 'decrease';
      }
      state.activeStep = action.payload;
    },
    setCitiesName: (state, action) => {
      state.cities = action.payload;
    },
    setStepOneData: (state, action) => {
      state.stepOneData = action.payload;
    },
    setStepTwoData: (state, action) => {
      state.stepTwoData = action.payload;
    },
    setStepFourData: (state, action) => {
      state.stepFourData = action.payload;
    },
    setStepFiveData: (state, action) => {
      state.stepFiveData = action.payload;
    },
    setStepSixData: (state, action) => {
      state.stepSixData = action.payload;
    },
    setStepSevenData: (state, action) => {
      state.stepSevenData = action.payload;
    },
    setResetRegisterSlice: () => initialState,
  },
});
export const {
  setActiveStep,
  setStepSevenData,
  setStepSixData,
  setStepFiveData,
  setStepFourData,
  setStepTwoData,
  setCitiesName,
  setStepOneData,
  setResetRegisterSlice,
} = CreateOrderSlice.actions;
export default CreateOrderSlice.reducer;
