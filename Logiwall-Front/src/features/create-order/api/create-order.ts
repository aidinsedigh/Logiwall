import { setActiveStep, setStepSevenData } from '../../../store/slices/create-order-slice';
import { useAppDispatch, useAppSelector } from '../../../store/store';
import useQueryParam from '../../../hooks/use-query-param';
import { useMutation, UseMutationResult } from '@tanstack/react-query';
import { axiosInternal } from '../../../lib/axios';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as Yup from 'yup';
import { isValidPhoneNumber } from 'react-phone-number-input';
import { NumberFieldSchema } from '../../../lib/yup/yupSchemas';
import * as yup from 'yup';
import { IcreateOrder, IcreateOrderResponse } from '../../../types/create-order';

//-------------------------------------------------------------------------
const schema: any = Yup.object().shape({
  receiverName: Yup.string()
    .matches(/^[\u0600-\u06FF\s]+$/, 'نام را بدرستی وارد کنید')
    .required('نام گیرنده را وارد کنید'),
  receiverLastName: Yup.string()
    .matches(/^[\u0600-\u06FF\s]+$/, 'نام خانوادگی را بدرستی وارد کنید')
    .required('نام خانوادگی گیرنده را وارد کنید'),
  receiverPhoneNumber: Yup.string()
    .test('isValidPhoneNumber', 'لطفا شماره تلفن معتبر وارد کنید', (value) => {
      return isValidPhoneNumber(value || '');
    })
    .required('شماره تلفن همراه خود را وارد کنید'),
  destinationAddress: Yup.string()
    .min(10, 'ادرس باید حداقل 10 کاراکتر داشته باشد')
    .required('ادرس  را وارد کنید'),
  destinationPlaque: NumberFieldSchema(
    'پلاک',
    1,
    199,
    'پلاک را بدرستی وارد نمایید',
    'پلاک را بدرستی وارد نمایید',
  ),
  destinationUnit: NumberFieldSchema(
    'واحد',
    1,
    199,
    'واحد را بدرستی وارد نمایید',
    'واحد را بدرستی وارد نمایید',
  ),
  destinationPostalCode: yup
    .string()
    .matches(/^\d*$/, 'کد پستی باید 10 رقمی باشد')
    .test('is-correct-length', 'کد پستی باید 10 رقمی باشد', (value) => {
      if (value && value.length > 0) {
        return value.length === 10;
      }
      return true;
    })
    .notRequired(),
});
//-------------------------------------------------------------------------

const useCreateOrder = () => {
  const selector: any = useAppSelector((state) => state.CreateOrderSlice);
  const { setQuery } = useQueryParam();
  const dispatch = useAppDispatch();

  const methods = useForm({
    resolver: yupResolver(schema),
    defaultValues: {
      receiverName: selector.stepSevenData.receiverName ?? '',
      receiverLastName: selector.stepSevenData.receiverLastName ?? '',
      receiverPhoneNumber: selector.stepSevenData.receiverPhoneNumber ?? '',
      destinationAddress: selector.stepSevenData.destinationAddress ?? '',
      destinationPlaque: selector.stepSevenData.destinationPlaque ?? '',
      destinationUnit: selector.stepSevenData.destinationUnit ?? '',
      destinationPostalCode: selector.stepSevenData.destinationPostalCode ?? '',
    },
  });
  const { handleSubmit } = methods;

  const { mutateAsync, isPending: CreateOrderPending } = useMutation({
    mutationFn: (payloads) =>
      axiosInternal.post('/api/Order/CreateOrder', payloads).then((res) => res.data.data),
  }) as UseMutationResult<IcreateOrderResponse>;

  const HandleSubmit = handleSubmit(async (payloads) => {
    dispatch(
      setStepSevenData({
        ...payloads,
        destinationPlaque: String(payloads.destinationPlaque),
        destinationUnit: String(payloads.destinationUnit),
        destinationPostalCode: String(payloads.destinationPostalCode),
      })
    );
    const mainPayload: IcreateOrder = {
      ...selector.stepFiveData,
      ...selector.stepFourData,
      ...selector.stepOneData,
      ...selector.stepTwoData,
      ...selector.stepSixData,
      ...payloads,
      destinationCountry: 1,
      channelId: 2,
      description: 'string',
      senderPhoneNumber: selector.stepFiveData.senderPhoneNumber.replace('+98', '0'),
      receiverPhoneNumber: payloads.receiverPhoneNumber.replace('+98', '0'),
    };
    console.log(mainPayload);
    try {
      const response: any = await mutateAsync(mainPayload);
      dispatch(setActiveStep(selector.activeStep + 1));
      setQuery({ orderId: response.orderId });
    } catch (error) {
      console.log(error);
    }
  });
  return { HandleSubmit, methods, CreateOrderPending };
};
export default useCreateOrder;
