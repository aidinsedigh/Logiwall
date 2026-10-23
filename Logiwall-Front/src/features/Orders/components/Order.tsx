import {
  Box,
  Stack,
  Step,
  StepConnector,
  stepConnectorClasses,
  StepIconProps,
  StepLabel,
  Stepper,
  styled,
  Typography,
  useTheme,
} from '@mui/material';
import { format } from 'date-fns-jalali';
import LgIcon from '../../../components/ui/icon';
import { FC } from 'react';

// -------------------------------------------------------------------------------
const QontoConnector = styled(StepConnector)(({ theme }) => ({
  [`&.${stepConnectorClasses.alternativeLabel}`]: {
    top: 10,
    left: 'calc(-50% + 16px)',
    right: 'calc(50% + 16px)',
  },
  [`&.${stepConnectorClasses.active}`]: {
    [`& .${stepConnectorClasses.line}`]: {
      borderColor: '#4bceaf',
    },
  },
  [`&.${stepConnectorClasses.completed}`]: {
    [`& .${stepConnectorClasses.line}`]: {
      borderColor: '#4bceaf',
    },
  },
  [`& .${stepConnectorClasses.line}`]: {
    borderColor: '#eaeaf0',
    borderTopWidth: 3,
    borderRadius: 1,
    ...theme.applyStyles('dark', {
      borderColor: theme.palette.grey[800],
    }),
  },
}));

const QontoStepIconRoot = styled('div')<{ ownerState: { active?: boolean } }>(({ theme }) => ({
  color: '#eaeaf0',
  display: 'flex',
  height: 22,
  alignItems: 'center',
  '& .QontoStepIcon-completedIcon': {
    color: '#4bceaf',
    zIndex: 1,
    fontSize: 18,
  },
  '& .QontoStepIcon-circle': {
    width: 8,
    height: 8,
    borderRadius: '50%',
    backgroundColor: 'currentColor',
  },
  ...theme.applyStyles('dark', {
    color: theme.palette.grey[700],
  }),
  variants: [
    {
      props: ({ ownerState }) => ownerState.active,
      style: {
        color: '#4bceaf',
      },
    },
  ],
}));

function QontoStepIcon(props: StepIconProps) {
  const { active, completed, className } = props;

  return (
    <QontoStepIconRoot ownerState={{ active }} className={className}>
      {completed ? (
        <LgIcon icon="solar:check-square-bold-duotone" className="text-customColors-primaryOne" />
      ) : (
        <div className="QontoStepIcon-circle" />
      )}
    </QontoStepIconRoot>
  );
}
// ------------------------------------------------------------------------------------------
interface Iorder {
  order: any;
}
// ------------------------------------------------------------------------------------------
const Order: FC<Iorder> = ({ order }) => {
  const theme = useTheme();
  return (
    <Box
      key={order?.id}
      className={`border ${theme.palette.mode === 'dark' ? 'bg-gray-700' : 'bg-customColors-primaryTwo'} w-full rounded-2xl border-none p-4 mt-4`}
    >
      <Stack spacing={2}>
        <Stack direction="row" justifyContent="space-between">
          <Stack spacing={2}>
            <Typography fontSize={15}> سفارش {order?.id}</Typography>
            <Typography color="gray">{order?.creationDate&&format(order?.creationDate, 'yyyy-MM-dd')}</Typography>
          </Stack>
          <Stack>
            <Box className="mt-2 bg-customColors-primaryOne rounded-2xl p-2">
              <Typography fontSize={11} color="white">
                {order?.statusName}
              </Typography>
            </Box>
          </Stack>
        </Stack>
        <Stepper alternativeLabel activeStep={1} connector={<QontoConnector />}>
          <Step>
            <StepLabel StepIconComponent={QontoStepIcon}>ثبت شده</StepLabel>
          </Step>
          <Step>
            <StepLabel StepIconComponent={QontoStepIcon}>بررسی</StepLabel>
          </Step>
          <Step>
            <StepLabel StepIconComponent={QontoStepIcon}>تایید شده</StepLabel>
          </Step>
          <Step>
            <StepLabel StepIconComponent={QontoStepIcon}> جمع اوری</StepLabel>
          </Step>
        </Stepper>
        <Stack direction="row" justifyContent="space-between">
          <Stack spacing={1}>
            <Typography color="gray">مبدا </Typography>
            <Typography>{order?.originCityName}</Typography>
          </Stack>
          <Stack spacing={1}>
            <Typography color="gray">مقصد </Typography>
            <Typography>{order?.destinationCityName}</Typography>
          </Stack>
        </Stack>
      </Stack>
      {/*{!watchMap[order.id] && <Stack textAlign='center' onClick={() => toggleMap(order.id)} className='text-customColors-primaryOne mt-5 cursor-pointer'>*/}
      {/*    <Typography>*/}
      {/*        مشاهده نقشه*/}
      {/*        <LgIcon icon='solar:arrow-left-line-duotone' className='inline-block'/>*/}
      {/*    </Typography>*/}
      {/*</Stack>}*/}
      {/*{watchMap[order.id] && <Stack mt={3} onClick={() => toggleMap(order.id)}><LgIcon className='text-red-700 cursor-pointer' icon='solar:close-square-bold-duotone'/></Stack>}*/}
      {/*{watchMap[order.id] && <Box><OrdersMap position2={[Number(order.destinationLat), Number(order.destinationLng)]} position1={[Number(order.originLat), Number(order.originLng)]}/></Box>}*/}
    </Box>
  );
};
export default Order;
