import { useMutation, UseMutationResult } from '@tanstack/react-query';
import { axiosInternal } from '../../../lib/axios';
import useApiEndpoints from '../../../hooks/use-api-endpoints';
import { useState } from 'react';
import useQueryParam from '../../../hooks/use-query-param';
import { useForm } from 'react-hook-form';
import { IgoPayPayload, IgoPayResponse } from '../../../types/create-order';
import { toast } from 'sonner';
import { useNavigate } from 'react-router';
import { useDispatch } from 'react-redux';
import { setResetRegisterSlice } from '../../../store/slices/create-order-slice';

//-----------------------------------------------------------------------------------

const usePay = () => {
  const [dialogStatus, setDialogStatus] = useState(false);
  const dispatch = useDispatch();
  const endpoints = useApiEndpoints();
  const { GetQuery } = useQueryParam();
  const navigate = useNavigate();
  const methods = useForm({
    defaultValues: {
      paymentType: 71,
      description: '',
    },
  });
  const { handleSubmit } = methods;

  // go to payment page request
  const { mutateAsync: GoToPay, isPending: payLoading } = useMutation({
    mutationKey: ['payment'],
    mutationFn: (payloads: any) =>
      axiosInternal
        .post(endpoints.CREATE_ORDER.POST_GO_PAYMENT, payloads)
        .then((res) => res?.data?.data),
  }) as UseMutationResult<IgoPayResponse>;

  const HandlePay = handleSubmit(async (payload) => {
    const mainPayload: IgoPayPayload = {
      paymentType: Number(payload.paymentType),
      orderId: Number(GetQuery?.orderId),
      pickupOptionId: Number(GetQuery?.pickupTime),
      channel: sessionStorage.getItem('channel') as string,
      callbackUrl: 'http://app.logiwall.ir/app/home',
    };
    try {
      if (payload.paymentType === 71) {
        const paymentResponse = await GoToPay(mainPayload);
        window.location.href = paymentResponse.paymentLink;
      } else {
        toast.success('با موفقیت انجام شد');
        setTimeout(() => {
          navigate('/app/home');
          dispatch(setResetRegisterSlice());
        }, 1000);
      }
    } catch (e) {
      console.log(e);
    }
  });

  return { GoToPay, methods, setDialogStatus, payLoading, HandlePay, dialogStatus, GetQuery };
};
export default usePay;
