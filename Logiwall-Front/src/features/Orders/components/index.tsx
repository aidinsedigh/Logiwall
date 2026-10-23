import { Box, Stack } from '@mui/material';
import { useState } from 'react';
import LgIcon from 'components/ui/icon';
import useOrders from '../api/orders';
import OrdersFilters from './orders-filters';
import { Pagination } from '@mui/material';
import LgSkeletonLoading from 'components/ui/loading/skeleton-loading';
import Order from './Order';
import NoDataFound from '../../../components/ui/no-data';
//--------------------------------------------------
const Orders = () => {
  const [dialogStatus, setDialogStatus] = useState(false);

  const { OrdersList, setPage, ordersFetching, filters, setFilters } = useOrders();
  return (
    <>
      {OrdersList && (
        <Box
          className="bg-customColors-primaryTwo w-11 h-11 text-white p-3 rounded-2xl text-center"
          onClick={() => setDialogStatus(true)}
        >
          <LgIcon icon="mage:filter" className="inline-block text-customColors-primaryOne" />
        </Box>
      )}
      {ordersFetching ? (
        <LgSkeletonLoading rowsNumber={2} />
      ) : !OrdersList ? (
        <NoDataFound />
      ) : (
        OrdersList?.result?.map((order: any) => <Order key={order?.id} order={order} />)
      )}
      {OrdersList && (
        <Stack textAlign="center" my={2}>
          <Pagination
            color="primary"
            count={Math.ceil(OrdersList?.totalCount / 3)}
            onChange={(e, n) => setPage(n)}
          />
        </Stack>
      )}
      {dialogStatus && (
        <OrdersFilters
          setPage={setPage}
          setFilters={setFilters}
          filters={filters}
          dialogStatus={dialogStatus}
          setDialogStatus={setDialogStatus}
        />
      )}
    </>
  );
};
export default Orders;
