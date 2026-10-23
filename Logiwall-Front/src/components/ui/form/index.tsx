import { FC } from 'react';
import { FormProvider as Form } from 'react-hook-form';

//-------------------------------------------------
interface IformProvider {
  methods: any;
  children: React.ReactNode;
  onSubmit: (values: any) => void;
}
//--------------------------------------------------

const FormProvider: FC<IformProvider> = ({ methods, children, onSubmit }) => {
  return (
    <Form {...methods}>
      <form className="h-full" onSubmit={onSubmit}>
        {children}
      </form>
    </Form>
  );
};
export default FormProvider;
