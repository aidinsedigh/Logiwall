import { Box, Stack, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import { Controller, useFieldArray, useForm } from 'react-hook-form';

import * as yup from 'yup';
import { useAppDispatch, useAppSelector } from 'store/store';
import { setActiveStep, setStepTwoData } from 'store/slices/create-order-slice';
import StepTitle from 'components/ui/title';
import FormProvider from 'components/ui/form';
import StepsButtons from 'components/ui/steps-buttons';
import { toast } from 'sonner';
import NumberField from 'components/ui/number-field';
import LgNumbericFormat from 'components/ui/numeric-format';
import { NumberFieldSchema } from 'lib/yup/yupSchemas';
import LgToggleButton from 'components/ui/toggle-button';
import { yupResolver } from '@hookform/resolvers/yup';

//-------------------------------------------------------------

const schema = yup.object().shape({
  orderParcels: yup.array().of(
    yup.object().shape({
      parcelTypeId: yup.number().required('نوع مرسوله الزامی است'),
      parcelValue: yup
        .string()
        .matches(/^\d+$/, 'ارزش مرسوله باید عددی باشد') // Ensures input contains only digits
        .required('ارزش مرسوله را وارد کنید') // Makes the field required
        .test('min-value', 'ارزش مرسوله حداقل 4,000,000 ریال میباشد', (value) => {
          if (value) return Number(value) >= 4000000;
          return true;
        })
        .test('max-value', 'ارزش مرسوله حداکثر 400,000,000 ریال میباشد', (value) => {
          if (value) return Number(value) <= 400000000;
          return true;
        }),
      weight: NumberFieldSchema(
        'وزن',
        1,
        6,
        'حداقل وزن 1 کیلوگرم میباشد',
        'حداکثر وزن 6 کیلوگرم میباشد'
      ),
      length: NumberFieldSchema(
        'طول',
        10,
        45,
        'حداقل طول 10 سانتی متر میباشد',
        'حداکثر طول 45 سانتی متر میباشد'
      ),
      width: NumberFieldSchema(
        'عرض',
        10,
        45,
        'حداقل عرض 10 سانتی متر میباشد',
        'حداکثر عرض 45 سانتی متر میباشد'
      ),
      height: NumberFieldSchema(
        'ارتفاع',
        10,
        45,
        'حداقل ارتفاع 10 سانتی متر میباشد',
        'حداکثر ارتفاع 45 سانتی متر میباشد'
      ),
      packageTypeId: yup.number().required('انتخاب نوع بسته‌بندی الزامی است'),
    })
  ),
});

//-------------------------------------------------------------------

const StepTwo = () => {
  // const [packaging, setPackaging] = useState<number | null>(1);
  const selector: any = useAppSelector((state) => state.CreateOrderSlice);

  const methods = useForm<any>({
    resolver: yupResolver(schema),
    defaultValues: {
      orderParcels: selector?.stepTwoData?.orderParcels ?? [
        {
          length: 0,
          weight: 0,
          packageTypeId: 0,
          parcelTypeId: 7,
          height: 0,
          width: 0,
          parcelValue: 0,
          cod: 0,
        },
      ],
    },
  });

  const { control, handleSubmit } = methods;
  const dispatch = useAppDispatch();

  const {
    fields: orderParcels,
    append,
    remove,
  } = useFieldArray({
    control,
    name: 'orderParcels',
  });

  const HandleSubmit = handleSubmit((payloads) => {
    payloads?.orderParcels?.map((e: any) => {
      if (e.parcelTypeId === 7 && e.weight > 2) {
        toast.error('حداکثر وزن برای پاکت 2 کیلوگرم میباشد');
      } else if (e.parcelTypeId === 8 && e.weight > 6) {
        toast.error('حداکثر وزن برای بسته 8 کیلوگرم میباشد');
      } else if (e.parcelTypeId === 7 && e.parcelValue > 10000000) {
        toast.error('حداکثر ارزش برای پاکت 10,000,000 ریال میباشد');
      } else if (e.parcelTypeId === 8 && e.parcelValue > 400000000) {
        toast.error('حداکثر ارزش برای پاکت 400,000,000 ریال میباشد');
      } else {
        e.weight = Number(e.weight);
        e.length = Number(e.length);
        e.width = Number(e.width);
        e.height = Number(e.height);
        e.parcelValue = Number(e.parcelValue);
        dispatch(setStepTwoData(payloads));
        dispatch(setActiveStep(selector.activeStep + 1));
      }
    });
  });

  return (
    <Box sx={{ height: '100%' }}>
      <StepTitle
        icon={'solar:info-circle-bold-duotone'}
        className="text-customColors-primaryOne"
        title={' لطفا اطلاعات مرسوله خود را وارد کنید'}
      />
      <FormProvider methods={methods} onSubmit={HandleSubmit}>
        {orderParcels.map((field, index) => (
          <Box key={index}>
            <Stack spacing={4} mb={5}>
              <LgToggleButton
                name={`orderParcels.${index}.parcelTypeId`}
                control={control}
                options={[
                  { label: 'پاکت', value: 7 },
                  { label: 'بسته', value: 8 },
                ]}
              />
              <LgNumbericFormat
                control={control}
                name={`orderParcels.${index}.parcelValue`}
                label="ارزش مرسوله"
              />
              <NumberField
                label="وزن"
                name={`orderParcels.${index}.weight`}
                endAdornment="کیلو گرم"
              />
              <NumberField
                label="طول"
                name={`orderParcels.${index}.length`}
                endAdornment="سانتی متر"
              />
              <NumberField
                label="عرض"
                name={`orderParcels.${index}.width`}
                endAdornment="سانتی متر"
              />
              <NumberField
                label="ارتفاع"
                name={`orderParcels.${index}.height`}
                endAdornment="سانتی متر"
              />
              <Controller
                name={`orderParcels.${index}.packageTypeId`}
                control={control}
                render={({ field }: any) => (
                  <ToggleButtonGroup
                    value={field.value}
                    exclusive
                    onChange={(event, newValue) => {
                      field.onChange(newValue);
                      // setPackaging(newValue);
                    }}
                    aria-label="text alignment"
                    fullWidth
                    color="primary"
                  >
                    <ToggleButton value={1} aria-label="right aligned">
                      <Typography fontSize={12}> مرسوله نیاز به بسته بندی دارد </Typography>
                    </ToggleButton>
                    <ToggleButton value={0} aria-label="justified">
                      <Typography fontSize={12}> ابعاد را دقیق نمیدانم </Typography>
                    </ToggleButton>
                  </ToggleButtonGroup>
                )}
              />
              {/*{packaging === 2 ? (*/}
              {/*  <LgSelect*/}
              {/*    name={`fields.${index}.typeOfParcel`}*/}
              {/*    control={control}*/}
              {/*    label="بسته بندی مرسوله"*/}
              {/*  >*/}
              {/*    <MenuItem key={'teh'} value={4}>*/}
              {/*      بسته بندی حباب دار*/}
              {/*    </MenuItem>*/}
              {/*    <MenuItem key={'ard'} value={5}>*/}
              {/*      بسته بندی کارتن A*/}
              {/*    </MenuItem>*/}
              {/*    <MenuItem key={'tab'} value={6}>*/}
              {/*      بسته بندی حباب دار بزرگ*/}
              {/*    </MenuItem>*/}
              {/*  </LgSelect>*/}
              {/*) : (*/}
              {/*  <Alert>*/}
              {/*    ابعاد مجاز مرسوله از 10 * 10 * 10 تا 45 * 45 * 45  سانتی متر می باشد*/}
              {/*  </Alert>*/}
              {/*)}*/}
            </Stack>
          </Box>
        ))}
        <Box
          className={`flex ${orderParcels.length === 1 ? 'justify-end' : 'justify-between'} mb-20`}
        >
          {orderParcels.length !== 1 && (
            <Box
              className="text-left cursor-pointer bg-red-700 text-white rounded-2xl p-3"
              onClick={() => {
                remove(orderParcels.length - 1);
              }}
            >
              حذف مرسوله
            </Box>
          )}
          <Box
            className="text-left cursor-pointer bg-customColors-primaryOne text-white rounded-2xl p-3"
            onClick={() => {
              append({
                parcelValue: '',
                parcelWeight: '',
                typeOfParcel: 4,
                parcelPackaging: '',
                parcelSize: '',
              });
              setTimeout(() => {
                window.scrollBy({ top: 500, behavior: 'smooth' });
              }, 100);
            }}
          >
            اضافه کردن مرسوله
          </Box>
        </Box>
        <StepsButtons />
      </FormProvider>
    </Box>
  );
};
export default StepTwo;
