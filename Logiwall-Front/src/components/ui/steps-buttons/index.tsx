import { Box, Button, useTheme } from '@mui/material';
import { useAppDispatch, useAppSelector } from '../../../store/store';
import { setActiveStep } from '../../../store/slices/create-order-slice';
import { FC } from 'react';
//---------------------------------------------------------------------------
interface IstepButtons {
  onClick?: () => void;
}
//---------------------------------------------------------------------------
const StepsButtons: FC<IstepButtons> = ({ onClick }) => {
  const dispatch = useAppDispatch();
  const selector = useAppSelector((state) => state.CreateOrderSlice.activeStep);
  const theme = useTheme();
  return (
    <Box
      className={`${theme.palette.mode === 'dark' ? 'bg-gray-800' : 'bg-white'} p-2 w-full fixed left-0.5 pb-5 bottom-0 z-10`}
    >
      <Box
        className={`flex ${selector === 8 ? 'justify-start' : selector === 0 ? 'justify-end' : 'justify-between'}`}
      >
        {selector !== 0 && (
          <Button
            onClick={() => dispatch(setActiveStep(selector - 1))}
            type="submit"
            sx={{
              borderRadius: 3,
              border: 'none',
              px: 6,
              color: '#4bceaf',
              backgroundColor: '#ecfffd',
              boxShadow: 'none',
            }}
            size="large"
            variant="contained"
          >
            مرحله قبل
          </Button>
        )}
        {selector !== 8 && (
          <Button
            type="submit"
            sx={{
              borderRadius: 3,
              border: 'none',
              px: 6,
              color: '#4bceaf',
              backgroundColor: '#ecfffd',
              boxShadow: 'none',
            }}
            size="large"
            variant="contained"
            onClick={onClick}
          >
            مرحله بعد
          </Button>
        )}
      </Box>
    </Box>
  );
};
export default StepsButtons;
