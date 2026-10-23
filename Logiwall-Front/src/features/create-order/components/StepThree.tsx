import { Box, Stack, Typography } from '@mui/material';
import { useForm } from 'react-hook-form';
import { useAppDispatch, useAppSelector } from '../../../store/store';
import { setActiveStep } from '../../../store/slices/create-order-slice';
import StepTitle from '../../../components/ui/title';
import FormProvider from '../../../components/ui/form';
import StepsButtons from '../../../components/ui/steps-buttons';

//----------------------------------------------------------------------
interface IkeyValue {
  title: string;
  value: string;
}
//-----------------------------------------------------------------------
function KeyValueComponent({ title, value }: IkeyValue) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
      <Typography fontSize={12}> {title} : </Typography>
      <Typography className="text-gray-500" fontSize={12}>
        {value}
      </Typography>
    </Box>
  );
}
//-----------------------------------------------------------------------
const StepThree = () => {
  const methods = useForm();
  const { handleSubmit } = methods;
  const dispatch = useAppDispatch();
  const selector: any = useAppSelector((state) => state.CreateOrderSlice);
  const HandleSubmit = handleSubmit(() => {
    dispatch(setActiveStep(selector.activeStep + 1));
  });
  return (
    <Box sx={{ height: '100%' }}>
      <StepTitle
        icon={'solar:info-square-bold-duotone'}
        className="text-customColors-primaryOne"
        title={'اطلاعات مرسوله'}
      />
      <FormProvider methods={methods} onSubmit={HandleSubmit}>
        {selector.stepTwoData.orderParcels?.map((data: any) => (
          <Stack key={data?.orderParcels?.length} pb={10} spacing={3}>
            <KeyValueComponent
              title="نوع مرسوله"
              value={data.parcelTypeId === 7 ? 'پاکت' : 'بسته'}
            />
            <KeyValueComponent
              title="بسته بندی"
              value={data.packageTypeId !== 0 ? 'دارد' : 'ندارد'}
            />
            <KeyValueComponent title="وزن" value={data.weight + ' کیلوگرم '} />
            <KeyValueComponent title="ارتفاع" value={data.height + ' سانتی متر '} />
            <KeyValueComponent title="طول" value={data.length + ' سانتی متر '} />
            <KeyValueComponent title="عرض" value={data.width + ' سانتی متر '} />
            <KeyValueComponent title="ارزش" value={data.parcelValue?.toLocaleString() + ' ریال '} />
          </Stack>
        ))}
        <StepsButtons />
      </FormProvider>
    </Box>
  );
};
export default StepThree;
