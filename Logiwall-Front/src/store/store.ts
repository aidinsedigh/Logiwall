import { configureStore } from '@reduxjs/toolkit';
import { TypedUseSelectorHook, useDispatch } from 'react-redux';
import { useSelector } from 'react-redux';
import Reducers from './reducers';

//----------------------------------------------------------------------

export const Store = configureStore({
  reducer: Reducers,
});
export type RootState = ReturnType<typeof Store.getState>;
export const useAppDispatch: () => typeof Store.dispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
