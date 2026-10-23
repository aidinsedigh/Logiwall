import { useMutation, UseMutationResult, useQuery } from '@tanstack/react-query';
import { axiosInternal } from 'lib/axios';
import useApiEndpoints from 'hooks/use-api-endpoints';
import useQueryParam from 'hooks/use-query-param';
import { useState } from 'react';
import { useAppDispatch, useAppSelector } from 'store/store';
import { setActiveStep } from 'store/slices/create-order-slice';
import { IgetOptions, IselectOption } from 'types/create-order';

// ---------------------------------------------------------------------

const useSelectOptions = () => {
  const [activeSlide, setActiveSlide] = useState(null);
  const [pickupTime, setPickupTime] = useState(null);

  const endpoints = useApiEndpoints();
  const { GetQuery, setQuery } = useQueryParam();
  const dispatch = useAppDispatch();
  const selector = useAppSelector((state) => state.CreateOrderSlice.activeStep);

  // get options list
  const { data: optionsList, isFetching: optionsFetching } = useQuery({
    queryKey: ['get-options'],
    queryFn: () =>
      axiosInternal
        .get(endpoints.CREATE_ORDER.GET_SERVICES_OPTIONS, {
          params: { orderId: GetQuery?.orderId },
        })
        .then((res) => res?.data?.data),
  });

  // get current provider option
  const filterProviderId = optionsList?.find(
    (data: IgetOptions) => data?.providerId === activeSlide
  );
  const defaultValueOfToggleButton = filterProviderId?.pickupDates?.[1]?.times?.[0]?.id || '';

  // select option request
  const { mutateAsync, isPending: SelectOptionPending } = useMutation({
    mutationKey: ['select-options'],
    mutationFn: (payloads: IselectOption) =>
      axiosInternal
        .post(endpoints.CREATE_ORDER.POST_SELECT_OPTIONS, payloads)
        .then((res) => res?.data?.data),
  }) as UseMutationResult<IselectOption>;

  const HandleSelectOption = async () => {
    try {
      const response = await mutateAsync({
        orderId: Number(GetQuery?.orderId),
        orderInqueryOptionId: filterProviderId?.orderOptions[0]?.id,
        pickupId: pickupTime ?? defaultValueOfToggleButton,
      });
      dispatch(setActiveStep(selector + 1));
      setQuery({
        ...GetQuery,
        pickupTime: response?.pickupOptions[0]?.id ?? defaultValueOfToggleButton,
      });
    } catch (err) {
      console.log(err);
    }
  };

  return {
    optionsList,
    pickupTime,
    SelectOptionPending,
    filterProviderId,
    optionsFetching,
    setPickupTime,
    setActiveSlide,
    HandleSelectOption,
    defaultValueOfToggleButton,
  };
};
export default useSelectOptions;
