import { Control, Controller, useFormContext } from 'react-hook-form';
import { TextField, TextFieldProps } from '@mui/material';
import { FC } from 'react';

//-------------------------------------------------------------------------------------
interface IlgSelect extends Omit<TextFieldProps, 'name' | 'control'> {
  name: string;
  control: Control<any>;
  native?: boolean;
  maxHeight?: number;
  PaperPropsSx?: any;
  helperText?: string;
  onChange?: (value: any) => void;
}
//-------------------------------------------------------------------------------------

const LgSelect: FC<IlgSelect> = ({
  name,
  native,
  maxHeight = 220,
  helperText,
  children,
  PaperPropsSx,
  onChange,
  ...other
}) => {
  const { control } = useFormContext();

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState: { error } }) => (
        <TextField
          {...field}
          select
          fullWidth
          onChange={onChange}
          sx={{ border: 'none' }}
          label="test"
          SelectProps={{
            native,
            MenuProps: {
              PaperProps: {
                sx: {
                  ...(!native && {
                    maxHeight: typeof maxHeight === 'number' ? maxHeight : 'unset',
                  }),
                  ...PaperPropsSx,
                },
              },
            },
            sx: { textTransform: 'capitalize' },
          }}
          error={!!error}
          helperText={error ? error?.message : helperText}
          {...other}
        >
          {children}
        </TextField>
      )}
    />
  );
};
export default LgSelect;
