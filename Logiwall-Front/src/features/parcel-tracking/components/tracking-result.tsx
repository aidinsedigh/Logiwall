import { Box, Stack, Step, StepLabel, Stepper, Typography } from '@mui/material';
import LgIcon from '../../../components/ui/icon';
import { useAppSelector } from '../../../store/store';

// ---------------------------------------------------------------------------

const TrackingResult = () => {
  const selector: any = useAppSelector((state) => state.TrackingSlice.trackingData);
  return (
    <Stack pb={5}>
      <Stack className="shadow-md border rounded-2xl p-5" spacing={4}>
        <Box className="flex justify-between">
          <Stack direction="row" spacing={1}>
            <Typography className="text-gray-500">
              <LgIcon
                icon="solar:map-point-bold-duotone"
                className="inline-block text-orange-400"
              />{' '}
              فرستنده :{' '}
            </Typography>
            <Typography>{selector?.SenderFullName}</Typography>
          </Stack>
          <Stack direction="row" spacing={1}>
            <Typography className="text-gray-500">
              <LgIcon
                icon="solar:map-point-bold-duotone"
                className="inline-block text-customColors-primaryOne"
              />{' '}
              گیرنده :{' '}
            </Typography>
            <Typography>{selector?.ReceiverFullName}</Typography>
          </Stack>
        </Box>
        <Box className="flex justify-center">
          <Stack direction="row" spacing={1}>
            <Typography className="text-gray-500">
              <LgIcon icon="duo-icons:building" className="inline-block" /> سرویس دهنده :{' '}
            </Typography>
            <Typography className="text-customColors-primaryOne">تیپاکس اکسپرس پارس</Typography>
          </Stack>
        </Box>
      </Stack>
      <Box mt={2} sx={{ maxWidth: '400px' }} className="shadow-md border p-3 rounded-2xl">
        <Stepper orientation="vertical" activeStep={selector?.states?.length}>
          {selector?.states?.map((data: any) => (
            <Step key={data?.StateNo}>
              <StepLabel>
                {' '}
                <Typography fontSize={12}>
                  {data?.StateTitle} ( {data?.OperationDateTime} ){' '}
                </Typography>
              </StepLabel>
            </Step>
          ))}
        </Stepper>
      </Box>
    </Stack>
  );
};
export default TrackingResult;
