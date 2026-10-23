import { useAppSelector } from '../../../store/store';
import StepOne from './StepOne';
import StepTwo from './StepTwo';
import StepThree from './StepThree';
import StepFour from './StepFour';
import StepFive from './StepFive';
import StepSix from './StepSix';
import StepSeven from './StepSeven';
import StepEight from './StepEight';
import {
  Box,
  Step,
  StepConnector,
  stepConnectorClasses,
  StepIconProps,
  StepLabel,
  Stepper,
  styled,
} from '@mui/material';
import { useEffect, useRef } from 'react';
import LgIcon from '../../../components/ui/icon';
import StepPay from './StepPay';

const ColorlibStepIconRoot = styled('div')<{
  ownerState: { completed?: boolean; active?: boolean };
}>(({ theme }) => ({
  backgroundColor: '#ccc',
  zIndex: 1,
  color: '#fff',
  width: 50,
  height: 50,
  display: 'flex',
  borderRadius: '50%',
  justifyContent: 'center',
  alignItems: 'center',
  ...theme.applyStyles('dark', {
    backgroundColor: theme.palette.grey[700],
  }),
  variants: [
    {
      props: ({ ownerState }) => ownerState.active,
      style: {
        backgroundImage: 'linear-gradient(90deg, rgba(79,117,255,1) 0%, rgba(75,206,175,1) 100%)',
        boxShadow: '0 4px 10px 0 rgba(0,0,0,.25)',
      },
    },
    {
      props: ({ ownerState }) => ownerState.completed,
      style: {
        backgroundImage: 'linear-gradient(90deg, rgba(79,117,255,1) 0%, rgba(75,206,175,1) 100%)',
      },
    },
  ],
}));
const ColorlibConnector = styled(StepConnector)(({ theme }: any) => ({
  [`&.${stepConnectorClasses.alternativeLabel}`]: {
    top: 22,
  },
  [`&.${stepConnectorClasses.active}`]: {
    [`& .${stepConnectorClasses.line}`]: {
      backgroundImage: 'linear-gradient(90deg, rgba(79,117,255,1) 0%, rgba(75,206,175,1) 100%)',
    },
  },
  [`&.${stepConnectorClasses.completed}`]: {
    [`& .${stepConnectorClasses.line}`]: {
      backgroundImage: 'linear-gradient(90deg, rgba(79,117,255,1) 0%, rgba(75,206,175,1) 100%)',
    },
  },
  [`& .${stepConnectorClasses.line}`]: {
    height: 3,
    border: 0,
    backgroundColor: '#eaeaf0',
    borderRadius: 1,
    ...theme.applyStyles('dark', {
      backgroundColor: theme.palette.grey[500],
    }),
  },
}));

function ColorlibStepIcon(props: StepIconProps) {
  const { active, completed, className } = props;

  const icons: { [index: string]: React.ReactElement<any> } = {
    1: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="solar:map-bold-duotone" />,
    2: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="lets-icons:form-fill" />,
    3: completed ? (
      <LgIcon icon="dashicons:yes" />
    ) : (
      <LgIcon icon="solar:letter-opened-bold-duotone" />
    ),
    4: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="solar:map-point-bold-duotone" />,
    5: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="eva:arrow-left-fill" />,
    6: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="solar:map-point-bold-duotone" />,
    7: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="eva:arrow-right-fill" />,
    8: completed ? <LgIcon icon="dashicons:yes" /> : <LgIcon icon="solar:earth-bold-duotone" />,
    9: completed ? (
      <LgIcon icon="dashicons:yes" />
    ) : (
      <LgIcon icon="solar:wallet-money-bold-duotone" />
    ),
  };

  return (
    <ColorlibStepIconRoot ownerState={{ completed, active }} className={className}>
      {icons[String(props.icon)]}
    </ColorlibStepIconRoot>
  );
}
//--------------------------------------------------------
const CreateOrder = () => {
  const boxRef = useRef<any>();
  const steps = [
    {
      label: 'انتخاب مبدا و مقصد',
      component: <StepOne />,
      key: 1,
    },
    {
      label: 'اطلاعات مرسوله',
      component: <StepTwo />,
      key: 2,
    },
    {
      label: 'نمایش اطلاعات مرسوله',
      component: <StepThree />,
      key: 3,
    },
    {
      label: 'لوکیشن فرستنده',
      component: <StepFour />,
      key: 4,
    },
    {
      label: 'اطلاعات فرستنده',
      component: <StepFive />,
      key: 5,
    },
    {
      label: 'لوکیشن گیرنده',
      component: <StepSix />,
      key: 6,
    },
    {
      label: 'اطلاعات گیرنده',
      component: <StepSeven />,
      key: 7,
    },
    {
      label: 'انتخاب سرویس',
      component: <StepEight />,
      key: 8,
    },
    {
      label: 'پرداخت',
      component: <StepPay />,
      key: 9,
    },
  ];
  const selector = useAppSelector((state) => state.CreateOrderSlice);
  useEffect(() => {
    if (selector.activeStepStatus === 'increase') {
      if (boxRef.current) {
        boxRef.current.scrollBy({ left: -100, behavior: 'smooth' });
      }
    } else {
      if (boxRef.current) {
        boxRef.current.scrollBy({ left: 100, behavior: 'smooth' });
      }
    }
  }, [selector.activeStep]);
  return (
    <>
      <Box
        ref={boxRef}
        sx={{
          overflowY: 'scroll',
          '&::-webkit-scrollbar': {
            display: 'none', // Hide scrollbar for Chrome, Safari, etc.
          },
          '-ms-overflow-style': 'none', // Hide scrollbar for IE and Edge
          'scrollbar-width': 'none', // Hide scrollbar for Firefox
          mb: 2,
        }}
      >
        <Stepper
          alternativeLabel
          connector={<ColorlibConnector />}
          activeStep={selector.activeStep}
        >
          {steps.map((step: any) => (
            <Step key={step.key}>
              <StepLabel
                className="w-24"
                StepIconComponent={ColorlibStepIcon}
                StepIconProps={{
                  sx: {
                    fontSize: '15px', // Customize the circle size
                  },
                }}
              >
                {step.label}
              </StepLabel>
            </Step>
          ))}
        </Stepper>
      </Box>
      {steps[selector.activeStep].component}
    </>
  );
};
export default CreateOrder;
