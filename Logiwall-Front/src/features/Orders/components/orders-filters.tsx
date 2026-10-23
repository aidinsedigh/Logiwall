import {
  Dialog,
  DialogContent,
  DialogTitle,
  Slide,
  Stack,
  Typography,
  useTheme,
} from '@mui/material';
import LgIcon from 'components/ui/icon';
import FormProvider from 'components/ui/form';
import LgInput from 'components/ui/input';
import { useForm } from 'react-hook-form';
import LgButton from 'components/ui/button';
import { FC, forwardRef } from 'react';
import { TransitionProps } from '@mui/material/transitions';
import SelectCity from 'components/basic-info/select-city';

// --------------------------------------------------------------------------------
const Transition = forwardRef(function Transition(
  props: TransitionProps & {
    children: React.ReactElement<unknown>;
  },
  ref: React.Ref<unknown>
) {
  return <Slide direction="up" ref={ref} {...props} />;
});

interface IordersFilters {
  dialogStatus: boolean;
  setDialogStatus: (status: boolean) => void;
  setFilters: (status: any) => void;
  setPage: (page: number) => void;
  filters: any;
}
// ---------------------------------------------------------------------------------
const OrdersFilters: FC<IordersFilters> = ({
  dialogStatus,
  filters,
  setPage,
  setDialogStatus,
  setFilters,
}) => {
  const theme = useTheme();

  const methods = useForm({
    defaultValues: {
      id: '',
      originCity: '',
      destinationCity: '',
      status: '',
    },
  });
  const { handleSubmit, control } = methods;
  const HandleFilters = handleSubmit((payloads) => {
    const conditions = Object.entries(payloads)
      .filter(([, value]) => value && value)
      .map(([key, value]) => ({ propertyName: key, value: Number(value), operator: 50 }));
    setDialogStatus(false);
    setFilters([
      {
        conditions: conditions,
        logicalOperator: 60,
      },
    ]);
  });

  const ResetFilters = () => {
    setFilters(undefined);
    setDialogStatus(false);
    setPage(1);
    methods.reset();
  };

  return (
    <Dialog
      fullScreen
      sx={{ mt: 10 }}
      open={dialogStatus}
      onClose={() => setDialogStatus(false)}
      TransitionComponent={Transition}
    >
      <DialogTitle className={`${theme.palette.mode === 'light' ? 'bg-white' : 'bg-gray-800'}`}>
        <Typography fontSize={20}>
          <LgIcon icon="mage:filter" className="inline-block" /> فیلتر
        </Typography>
      </DialogTitle>
      <DialogContent
        className={`${theme.palette.mode === 'light' ? 'bg-white' : 'bg-gray-800'}`}
        sx={{ p: 1 }}
      >
        <FormProvider methods={methods} onSubmit={HandleFilters}>
          <Stack pt={2.5} className="h-full flex flex-col justify-between">
            <Stack spacing={3}>
              <LgInput label="شماره سفارش" name="id" />
              <LgInput label="وضعیت" name="status" />
              <SelectCity control={control} isOrigin name="originCity" />
              <SelectCity control={control} isOrigin={false} name="destinationCity" />
            </Stack>
            <Stack direction="row" justifyContent="space-between">
              <LgButton type="button" variant="text" sx={{ color: 'red' }} onClick={ResetFilters}>
                {' '}
                حذف همه فیلتر ها
                <LgIcon
                  icon="solar:trash-bin-minimalistic-bold-duotone"
                  width={15}
                  className="mx-0.5 inline-block"
                />{' '}
              </LgButton>
              <LgButton>فیلتر کردن !</LgButton>
            </Stack>
          </Stack>
        </FormProvider>
      </DialogContent>
    </Dialog>
  );
};
export default OrdersFilters;
