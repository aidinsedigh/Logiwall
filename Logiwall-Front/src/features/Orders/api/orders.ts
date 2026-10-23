import { useCallback, useState } from 'react';
import useApiEndpoints from '../../../hooks/use-api-endpoints';
import { useTheme } from '@mui/material';
import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { axiosInternal } from 'lib/axios';
import { useLocation } from 'react-router-dom';
import { IorderResponse } from '../../../types/orders';

// ------------------------------------------------------------------------

const useOrders = () => {
  const [watchMap, setWatchMap] = useState<any>(false);
  const [filters, setFilters] = useState<any>();
  const [page, setPage] = useState(1);
  const endpoints = useApiEndpoints();
  const theme = useTheme();
  const location = useLocation();

  const { data: OrdersList, isFetching: ordersFetching } = useQuery({
    queryKey: ['orders-list', filters, page],
    queryFn: () =>
      axiosInternal
        .post(endpoints.GET_ORDERS.GET_ORDERS_LIST, {
          hasPagination: true,
          pageNumber: page ?? 1,
          pageSize: 3,
          ...{ filterGroups: filters },
        })
        .then((res) => res?.data?.data),
    enabled: location.pathname === '/app/orders',
  }) as UseQueryResult<IorderResponse>;

  const toggleMap = useCallback(
    (id: number) => {
      setWatchMap((prevOpenMaps: any) => ({
        ...prevOpenMaps,
        [id]: !prevOpenMaps[id],
      }));
    },
    [watchMap]
  );

  return { toggleMap, OrdersList, ordersFetching, setPage, theme, watchMap, setFilters, filters };
};
export default useOrders;
